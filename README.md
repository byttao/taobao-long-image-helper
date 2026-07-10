# 淘宝商品长图提取工具（.NET 8 / C# GUI）

版本：1.5.0  
日期：2026-07-10

## 环境要求

- Windows
- .NET 8 Desktop Runtime
- Google Chrome

程序不打包 .NET Desktop Runtime。发布目录会包含程序自身和 `Microsoft.Playwright` 依赖。

## 开源协议

本项目使用 MIT License。详见 [LICENSE](LICENSE)。

## 本机开发运行

启动 GUI：

```powershell
dotnet run
```

界面操作：

1. 检查或手动选择 Chrome 路径。
2. 点击“初始化”，程序会自动选择可用端口，启动 Chrome，并打开淘宝登录页。
3. 在 Chrome 窗口中完成淘宝扫码登录。
4. 设置图片保存文件夹。
5. 输入商品ID或商品链接，点击“开始获取长图”。
6. 完成任务可从列表拖拽到本地文件夹，系统会复制对应图片。
7. 如果拖拽不可用，可双击完成任务行，把图片文件复制到剪贴板，再到目标文件夹粘贴。

## 发布给其他设备

```powershell
.\publish-win-x64.ps1
```

发布目录：

```text
publish-win-x64
```

把该目录复制到其他 Windows 设备后运行：

```powershell
.\TaobaoLongImageHelper.exe
```

## 说明

- 程序会定位本机 Chrome 的常见安装路径。
- 可在界面中手动修改 Chrome 路径。
- 初始化时如果端口被占用，会自动调整到下一个可用端口。
- 可在界面中设置图片保存文件夹。
- 页面关键操作之间会加入固定范围内的随机等待，降低连续快速访问导致的误判风险。
- 商品标题会优先从 `span[class^="mainTitle"]` 的 `title` 属性提取；没有 `title` 时依次尝试 `value` 和文本。
- 图片定位只使用商品名称拼接店铺搜索页，不使用商品ID作为搜索词。
- 如果店铺搜索页没有返回目标商品，程序会明确提示，并使用商品详情页主图兜底。
- 因为连接的是本机已安装 Chrome，不需要下载 Playwright 自带浏览器。
- 不要把账号密码、Cookie、Session Token 或其他凭据提交到仓库。
