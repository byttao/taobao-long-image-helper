using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

public sealed class TaobaoExtractor
{
    public async Task<ExtractResult> ExtractAsync(
        string input,
        int port,
        string outputDir,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var productUrl = NormalizeInput(input);
        var productId = ExtractProductId(productUrl);
        if (string.IsNullOrWhiteSpace(productUrl) || string.IsNullOrWhiteSpace(productId))
        {
            throw new ArgumentException("请输入淘宝商品ID，或包含 id 参数的淘宝商品链接。");
        }

        progress?.Report($"商品ID：{productId}");
        progress?.Report($"正在连接 Chrome：http://127.0.0.1:{port}");

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.ConnectOverCDPAsync(
            $"http://127.0.0.1:{port}",
            new BrowserTypeConnectOverCDPOptions
            {
                IsLocal = true,
                NoDefaults = true,
                Timeout = 15000
            });

        cancellationToken.ThrowIfCancellationRequested();
        var context = browser.Contexts.FirstOrDefault() ?? await browser.NewContextAsync();
        var page = await context.NewPageAsync();
        var productName = "";
        var sellerId = "";
        var shopId = "";
        var diagnosticAuctionUrl = "";

        try
        {
            await RandomOperationDelayAsync(cancellationToken, progress, "准备打开商品页");
            await NavigateAsync(page, productUrl);
            await WaitForPageReadyAsync(page);
            await RandomOperationDelayAsync(cancellationToken, progress, "商品页已加载");

            if (await IsLoginPageAsync(page))
            {
                progress?.Report("检测到淘宝登录页，请在 Chrome 中扫码登录；程序会自动等待。");
                await WaitForLoginAsync(page, productUrl, cancellationToken);
                await RandomOperationDelayAsync(cancellationToken, progress, "登录完成，准备读取商品信息");
            }

            cancellationToken.ThrowIfCancellationRequested();
            productName = await ExtractProductNameAsync(page);
            if (string.IsNullOrWhiteSpace(productName))
            {
                throw new InvalidOperationException("未能提取商品名称。");
            }
            progress?.Report($"商品名称：{productName}");

            (sellerId, shopId) = await ExtractSellerAndShopIdsAsync(page);

            if (string.IsNullOrWhiteSpace(sellerId) || string.IsNullOrWhiteSpace(shopId))
            {
                var shopHomeUrl = await ExtractShopHomeUrlAsync(page);
                if (string.IsNullOrWhiteSpace(shopHomeUrl))
                {
                    throw new InvalidOperationException("未能找到店铺首页入口，无法继续提取 sellerId/shopId。");
                }

                progress?.Report("正在打开店铺首页提取 sellerId/shopId。");
                await RandomOperationDelayAsync(cancellationToken, progress, "准备打开店铺首页");
                await NavigateAsync(page, shopHomeUrl);
                await WaitForPageReadyAsync(page);
                await RandomOperationDelayAsync(cancellationToken, progress, "店铺首页已加载");
                (sellerId, shopId) = await ExtractSellerAndShopIdsAsync(page);
            }

            if (string.IsNullOrWhiteSpace(sellerId) || string.IsNullOrWhiteSpace(shopId))
            {
                throw new InvalidOperationException($"未能提取 sellerId/shopId。sellerId={sellerId ?? "空"} shopId={shopId ?? "空"}");
            }

            progress?.Report($"sellerId：{sellerId}");
            progress?.Report($"shopId：{shopId}");

            var auctionUrl = BuildAuctionUrl(sellerId, shopId, productName);
            diagnosticAuctionUrl = auctionUrl;
            progress?.Report($"正在打开店铺搜索页定位图片，搜索词：{productName}");
            await RandomOperationDelayAsync(cancellationToken, progress, "准备打开店铺搜索页");
            await NavigateAsync(page, auctionUrl);
            await WaitForPageReadyAsync(page);
            await RandomOperationDelayAsync(cancellationToken, progress, "店铺搜索页已加载，准备定位图片");

            var rawImageUrl = "";
            try
            {
                rawImageUrl = await FindAuctionImageUrlAsync(page, productId);
            }
            catch (Exception ex) when (ex is TimeoutException || ex.Message.Contains("Timeout", StringComparison.OrdinalIgnoreCase))
            {
                progress?.Report("店铺搜索页未返回目标商品。");
            }

            if (string.IsNullOrWhiteSpace(rawImageUrl))
            {
                throw new InvalidOperationException(
                    $"店铺搜索页未定位到目标商品，未下载图片。商品名称={productName}；sellerId={sellerId}；shopId={shopId}");
            }

            var imageUrl = CleanImageUrl(rawImageUrl);
            Directory.CreateDirectory(outputDir);
            var outputFile = Path.Combine(outputDir, $"{productId}长图.jpg");
            await RandomOperationDelayAsync(cancellationToken, progress, "准备下载图片", 1200, 3000);
            await DownloadImageAsync(imageUrl, outputFile, cancellationToken);

            progress?.Report($"图片链接：{imageUrl}");
            progress?.Report($"已保存：{outputFile}");

            return new ExtractResult
            {
                ProductId = productId,
                ProductName = productName,
                SellerId = sellerId,
                ShopId = shopId,
                ImageUrl = imageUrl,
                OutputFile = outputFile
            };
        }
        catch (Exception ex) when (
            ex is not ExtractDiagnosticException
            && ex is not OperationCanceledException
            && HasDiagnosticInfo(diagnosticAuctionUrl, productName, sellerId, shopId))
        {
            throw new ExtractDiagnosticException(ex.Message, diagnosticAuctionUrl, productName, sellerId, shopId, ex);
        }
        finally
        {
            await page.CloseAsync(new PageCloseOptions { RunBeforeUnload = false }).CatchAsync();
        }
    }

    public static string NormalizeInput(string? input)
    {
        var trimmed = (input ?? "").Trim();
        if (trimmed.Length == 0) return "";
        if (Regex.IsMatch(trimmed, @"^\d+$")) return $"https://item.taobao.com/item.htm?id={trimmed}";
        if (trimmed.StartsWith("//", StringComparison.Ordinal)) return $"https:{trimmed}";
        if (!Regex.IsMatch(trimmed, "^https?://", RegexOptions.IgnoreCase)) return $"https://{trimmed}";
        return Regex.Replace(trimmed, "^http://", "https://", RegexOptions.IgnoreCase);
    }

    public static string ExtractProductId(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            var query = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in query)
            {
                var parts = pair.Split('=', 2);
                var key = Uri.UnescapeDataString(parts[0]);
                var value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : "";
                if (new[] { "id", "itemId", "item_id", "auctionId" }.Contains(key, StringComparer.OrdinalIgnoreCase)
                    && Regex.IsMatch(value, @"^\d+$"))
                {
                    return value;
                }
            }
        }

        var match = Regex.Match(url, @"[?&](?:id|itemId|item_id|auctionId)=(\d+)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : "";
    }

    private static bool HasDiagnosticInfo(string diagnosticUrl, string productName, string sellerId, string shopId)
    {
        return !string.IsNullOrWhiteSpace(diagnosticUrl)
            || !string.IsNullOrWhiteSpace(productName)
            || !string.IsNullOrWhiteSpace(sellerId)
            || !string.IsNullOrWhiteSpace(shopId);
    }

    private static async Task NavigateAsync(IPage page, string url)
    {
        Exception? lastError = null;
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                await page.GotoAsync(url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = 60000
                });
                return;
            }
            catch (Exception ex) when (IsRecoverableNavigationAbort(ex, page))
            {
                await WaitForPageReadyAsync(page);
                return;
            }
            catch (Exception ex) when (attempt < 3)
            {
                lastError = ex;
                await Task.Delay(RandomDelay(2500, 5500));
            }
            catch (Exception ex)
            {
                lastError = ex;
            }
        }

        throw lastError ?? new InvalidOperationException("页面打开失败。");
    }

    private static bool IsRecoverableNavigationAbort(Exception ex, IPage page)
    {
        return ex.Message.Contains("ERR_ABORTED", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(page.Url)
            && !string.Equals(page.Url, "about:blank", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task WaitForPageReadyAsync(IPage page)
    {
        try
        {
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded, new PageWaitForLoadStateOptions { Timeout = 45000 });
        }
        catch
        {
        }

        try
        {
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 12000 });
        }
        catch
        {
        }
    }

    private static async Task WaitForLoginAsync(IPage page, string productUrl, CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow.AddMinutes(10);
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(RandomDelay(3000, 6000), cancellationToken);
            if (!await IsLoginPageAsync(page))
            {
                await NavigateAsync(page, productUrl);
                await WaitForPageReadyAsync(page);
                return;
            }
        }

        throw new TimeoutException("等待淘宝扫码登录超时。");
    }

    private static async Task RandomOperationDelayAsync(
        CancellationToken cancellationToken,
        IProgress<string>? progress = null,
        string? message = null,
        int minMilliseconds = 1800,
        int maxMilliseconds = 4200)
    {
        var delay = RandomDelay(minMilliseconds, maxMilliseconds);
        if (!string.IsNullOrWhiteSpace(message))
        {
            progress?.Report($"{message}，等待 {delay.TotalSeconds:F1} 秒。");
        }

        await Task.Delay(delay, cancellationToken);
    }

    private static TimeSpan RandomDelay(int minMilliseconds, int maxMilliseconds)
    {
        if (maxMilliseconds <= minMilliseconds)
        {
            return TimeSpan.FromMilliseconds(minMilliseconds);
        }

        return TimeSpan.FromMilliseconds(RandomNumberGenerator.GetInt32(minMilliseconds, maxMilliseconds + 1));
    }

    private static async Task<bool> IsLoginPageAsync(IPage page)
    {
        return await page.EvaluateAsync<bool>(
            @"() => {
                const bodyText = (document.body?.innerText || '').replace(/\s+/g, ' ').slice(0, 3000);
                return location.hostname.includes('login.taobao.com')
                    || location.href.includes('/member/login')
                    || /扫码登录|密码登录|手机扫码|安全登录/.test(bodyText);
            }");
    }

    private static async Task<string> ExtractProductNameAsync(IPage page)
    {
        return await page.EvaluateAsync<string>(
            @"() => {
                const textOf = element => (element?.textContent || '').replace(/\s+/g, ' ').trim();
                const attr = (element, name) => element?.getAttribute(name) || '';
                const clean = value => (value || '')
                    .replace(/[-_]\s*(淘宝网|天猫|tmall\.com|Taobao).*$/i, '')
                    .replace(/\s+/g, ' ')
                    .trim();
                const mainTitle = document.querySelector('span[class^=""mainTitle""]');
                if (mainTitle) {
                    const value = mainTitle.getAttribute('title')
                        || mainTitle.getAttribute('value')
                        || textOf(mainTitle);
                    if (value) return clean(value);
                }
                const selectors = [
                    'meta[property=""og:title""]',
                    'meta[name=""title""]',
                    'h1',
                    '.tb-main-title',
                    '[class*=""ItemHeader--mainTitle--""]',
                    '[class*=""ItemTitle""]',
                    '[class*=""mainTitle""]'
                ];

                for (const selector of selectors) {
                    const element = document.querySelector(selector);
                    const value = selector.startsWith('meta') ? attr(element, 'content') : textOf(element);
                    if (value) return clean(value);
                }

                return clean(document.title);
            }");
    }

    private static async Task<(string SellerId, string ShopId)> ExtractSellerAndShopIdsAsync(IPage page)
    {
        var result = await page.EvaluateAsync<SellerShopIds>(
            @"() => {
                const scripts = Array.from(document.scripts).map(script => script.textContent || '').join('\n');
                const html = `${location.href}\n${document.documentElement.innerHTML}\n${scripts}`;
                const collect = name => {
                    const patterns = [
                        new RegExp(`[?&]${name}=([0-9]{4,})`, 'gi'),
                        new RegExp(`[""']${name}[""']\\s*[:=]\\s*[""']?([0-9]{4,})`, 'gi'),
                        new RegExp(`${name}\\s*[:=]\\s*[""']?([0-9]{4,})`, 'gi')
                    ];
                    const values = [];
                    for (const pattern of patterns) {
                        for (const match of html.matchAll(pattern)) {
                            if (match?.[1]) values.push(match[1]);
                        }
                    }
                    return values;
                };
                const first = (values, rejects) => values.find(value => !rejects.includes(value)) || '';
                const hostnameShopId = (location.hostname.match(/^shop(\d+)\.taobao\.com$/i) || [])[1] || '';
                const sellerId = first([...collect('sellerId'), ...collect('sellerid')], []);
                const shopRejects = ['1', '-1', sellerId].filter(Boolean);
                const shopId = first([hostnameShopId, ...collect('shopId'), ...collect('shopid')].filter(Boolean), shopRejects);
                return { sellerId, shopId };
            }");

        return (result.SellerId ?? "", result.ShopId ?? "");
    }

    private static async Task<string> ExtractShopHomeUrlAsync(IPage page)
    {
        return await page.EvaluateAsync<string>(
            @"() => {
                const textOf = element => (element?.textContent || '').replace(/\s+/g, ' ').trim();
                const absolutize = href => {
                    if (!href) return '';
                    if (href.startsWith('//')) return `${location.protocol}${href}`;
                    try { return new URL(href, location.href).href; } catch { return ''; }
                };
                const links = Array.from(document.querySelectorAll('a[href]'));
                const byLabel = links.find(link => {
                    const label = textOf(link);
                    return label.includes('店铺首页') || label === '进店' || label.endsWith('进店');
                });
                if (byLabel) return absolutize(byLabel.getAttribute('href') || '');

                const byHost = links.find(link => {
                    const href = link.getAttribute('href') || '';
                    return /\/\/shop\d+\.taobao\.com\//i.test(href) || /\/\/[^/]+\.tmall\.com\/category/i.test(href);
                });
                return absolutize(byHost?.getAttribute('href') || '');
            }");
    }

    private static async Task<string> FindAuctionImageUrlAsync(IPage page, string productId)
    {
        await page.WaitForFunctionAsync(
            @"id => Array.from(document.querySelectorAll('[href*=""id=""]')).some(element => {
                const href = element.getAttribute('href') || '';
                return href.includes(`id=${id}`) && (element.querySelector('img') || element.closest('div')?.querySelector('img'));
            })",
            productId,
            new PageWaitForFunctionOptions { Timeout = 25000 });

        return await page.EvaluateAsync<string>(
            @"id => {
                const absolutize = url => {
                    if (!url) return '';
                    if (url.startsWith('//')) return `${location.protocol}${url}`;
                    try { return new URL(url, location.href).href; } catch { return url; }
                };
                const imageUrlFromElement = element => {
                    if (!element) return '';
                    const direct = element.currentSrc
                        || element.getAttribute('src')
                        || element.getAttribute('data-src')
                        || element.getAttribute('data-ks-lazyload')
                        || element.getAttribute('data-lazyload-src')
                        || element.getAttribute('data-img');
                    if (direct) return absolutize(direct);

                    const style = element.getAttribute('style') || '';
                    const background = (style.match(/url\([""']?([^""')]+)[""']?\)/i) || [])[1];
                    return absolutize(background || '');
                };
                const elements = Array.from(document.querySelectorAll('[href*=""id=""]'));
                for (const element of elements) {
                    const href = absolutize(element.getAttribute('href') || '');
                    if (!href.includes(`id=${id}`)) continue;

                    const img = element.querySelector('img')
                        || element.closest('div')?.querySelector('img')
                        || element.parentElement?.querySelector('img');
                    const imgSrc = imageUrlFromElement(img);
                    if (imgSrc) return imgSrc;
                }
                return '';
            }",
            productId);
    }

    private static string BuildAuctionUrl(string sellerId, string shopId, string productName)
    {
        var builder = new UriBuilder("https://market.m.taobao.com/app/tb-source-app/shop-auction/pages/auction");
        builder.Query = $"wh_weex=true&sellerId={Uri.EscapeDataString(sellerId)}&shopId={Uri.EscapeDataString(shopId)}&searchText={Uri.EscapeDataString(productName)}";
        return builder.Uri.ToString();
    }

    private static async Task DownloadImageAsync(string imageUrl, string outputFile, CancellationToken cancellationToken)
    {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Referrer = new Uri("https://market.m.taobao.com/");
        http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Mozilla", "5.0"));
        using var response = await http.GetAsync(imageUrl, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"图片下载失败：HTTP {(int)response.StatusCode}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var file = File.Create(outputFile);
        await stream.CopyToAsync(file, cancellationToken);
    }

    private static string CleanImageUrl(string? url)
    {
        var value = (url ?? "").Replace("http://", "https://", StringComparison.OrdinalIgnoreCase);
        if (value.StartsWith("//", StringComparison.Ordinal)) value = $"https:{value}";
        value = Regex.Replace(value, @"_q\d+\.jpg_\.webp$", "", RegexOptions.IgnoreCase);
        value = Regex.Replace(value, @"_\.webp$", "", RegexOptions.IgnoreCase);
        value = Regex.Replace(value, @"_360x360\.jpg$", "", RegexOptions.IgnoreCase);
        value = Regex.Replace(value, @"_360x360q\d+\.jpg(?:_\.webp)?$", "", RegexOptions.IgnoreCase);
        return value;
    }

    private sealed class SellerShopIds
    {
        public string? SellerId { get; set; }
        public string? ShopId { get; set; }
    }
}

internal static class TaskExtensions
{
    public static async Task CatchAsync(this Task task)
    {
        try
        {
            await task;
        }
        catch
        {
        }
    }
}
