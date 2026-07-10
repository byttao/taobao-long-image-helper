using System.Net;
using System.Text.RegularExpressions;

public static class SourceParser
{
    public static string BuildQianniuProductUrl(string productId)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            throw new ArgumentException("缺少商品ID。");
        }

        return $"https://detail.tmall.com/item.htm?b_s_f=sycm&b_spm=a21ag.29085015&b_spm_log=40c150a5aXPjnb&id={Uri.EscapeDataString(productId)}";
    }

    public static string ExtractProductName(string html)
    {
        var source = html ?? "";
        foreach (Match match in Regex.Matches(source, @"<span\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Singleline))
        {
            var tag = match.Value;
            var className = GetAttribute(tag, "class");
            if (!className.StartsWith("mainTitle", StringComparison.OrdinalIgnoreCase)
                && !className.Contains("mainTitle", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = FirstNonEmpty(GetAttribute(tag, "title"), GetAttribute(tag, "value"));
            if (!string.IsNullOrWhiteSpace(value))
            {
                return CleanTitle(value);
            }

            var closeIndex = source.IndexOf("</span>", match.Index + match.Length, StringComparison.OrdinalIgnoreCase);
            if (closeIndex > match.Index)
            {
                var innerHtml = source.Substring(match.Index + match.Length, closeIndex - match.Index - match.Length);
                value = StripTags(innerHtml);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return CleanTitle(value);
                }
            }
        }

        var jsonTitle = MatchFirst(source,
            @"""title""\s*:\s*""([^""]{4,200})""",
            @"""itemTitle""\s*:\s*""([^""]{4,200})""",
            @"""mainTitle""\s*:\s*""([^""]{4,200})""");
        if (!string.IsNullOrWhiteSpace(jsonTitle))
        {
            return CleanTitle(jsonTitle);
        }

        return "";
    }

    public static (string SellerId, string ShopId) ExtractSellerAndShopIds(string html)
    {
        var source = html ?? "";
        var sellerId = FirstId(CollectIds(source, "sellerId").Concat(CollectIds(source, "sellerid")));
        var rejects = new[] { "1", "-1", sellerId }.Where(value => !string.IsNullOrWhiteSpace(value)).ToHashSet();
        var shopId = FirstId(CollectIds(source, "shopId").Concat(CollectIds(source, "shopid")), rejects);
        return (sellerId, shopId);
    }

    private static IEnumerable<string> CollectIds(string source, string name)
    {
        var patterns = new[]
        {
            $@"[?&]{Regex.Escape(name)}=([0-9]{{4,}})",
            $@"""?{Regex.Escape(name)}""?\s*[:=]\s*""?([0-9]{{4,}})",
            $@"'{Regex.Escape(name)}'\s*[:=]\s*'?([0-9]{{4,}})"
        };

        foreach (var pattern in patterns)
        {
            foreach (Match match in Regex.Matches(source, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline))
            {
                yield return match.Groups[1].Value;
            }
        }
    }

    private static string FirstId(IEnumerable<string> values, ISet<string>? rejects = null)
    {
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value)) continue;
            if (rejects?.Contains(value) == true) continue;
            return value;
        }

        return "";
    }

    private static string GetAttribute(string tag, string name)
    {
        var match = Regex.Match(
            tag,
            $@"\b{Regex.Escape(name)}\s*=\s*(['""])(.*?)\1",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return match.Success ? WebUtility.HtmlDecode(match.Groups[2].Value).Trim() : "";
    }

    private static string MatchFirst(string source, params string[] patterns)
    {
        foreach (var pattern in patterns)
        {
            var match = Regex.Match(source, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (match.Success)
            {
                return WebUtility.HtmlDecode(Regex.Unescape(match.Groups[1].Value)).Trim();
            }
        }

        return "";
    }

    private static string FirstNonEmpty(params string[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "";
    }

    private static string StripTags(string html)
    {
        return WebUtility.HtmlDecode(Regex.Replace(html, "<.*?>", "", RegexOptions.Singleline)).Trim();
    }

    private static string CleanTitle(string value)
    {
        var cleaned = WebUtility.HtmlDecode(value ?? "");
        cleaned = Regex.Replace(cleaned, @"[-_]\s*(淘宝网|天猫|tmall\.com|Taobao).*$", "", RegexOptions.IgnoreCase);
        cleaned = Regex.Replace(cleaned, @"\s+", " ");
        return cleaned.Trim();
    }
}
