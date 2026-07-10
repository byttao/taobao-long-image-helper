using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

public static class ChromeHelper
{
    public const int DefaultPort = 9222;
    public const string LoginUrl = "https://login.taobao.com/havanaone/login/login.htm";

    public static string? FindChromePath()
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application", "chrome.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "Application", "chrome.exe")
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    public static int FindAvailablePort(int preferredPort = DefaultPort)
    {
        for (var port = preferredPort; port < preferredPort + 200; port++)
        {
            if (IsPortAvailable(port)) return port;
        }

        throw new InvalidOperationException("未找到可用监听端口。");
    }

    public static bool IsPortAvailable(int port)
    {
        try
        {
            using var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static string GetDefaultProfileDir()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CodexTaobaoPlaywrightChromeProfile");
    }

    public static Process? StartChrome(string chromePath, int port, string? profileDir = null)
    {
        if (!File.Exists(chromePath))
        {
            throw new FileNotFoundException("Chrome 路径不存在。", chromePath);
        }

        profileDir ??= GetDefaultProfileDir();
        Directory.CreateDirectory(profileDir);

        var psi = new ProcessStartInfo
        {
            FileName = chromePath,
            UseShellExecute = false
        };
        psi.ArgumentList.Add($"--remote-debugging-port={port}");
        psi.ArgumentList.Add($"--user-data-dir={profileDir}");
        psi.ArgumentList.Add("--no-first-run");
        psi.ArgumentList.Add("--no-default-browser-check");
        psi.ArgumentList.Add(LoginUrl);

        return Process.Start(psi);
    }
}
