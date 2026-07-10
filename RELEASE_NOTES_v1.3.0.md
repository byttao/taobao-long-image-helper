# TaobaoLongImageHelper v1.3.0

发布日期：2026-07-10

## 本次发布

- 创建规范化 GitHub 开源仓库。
- 添加 MIT License。
- 添加中文更新日志。
- 发布 Windows x64 framework-dependent 版本。
- 发布包不包含 .NET Desktop Runtime；目标设备需自行安装 .NET 8 Desktop Runtime。

## 使用说明

1. 解压 `TaobaoLongImageHelper-win-x64.zip`。
2. 运行 `TaobaoLongImageHelper.exe`。
3. 点击“初始化”，启动 Chrome 并打开淘宝登录页。
4. 用户在 Chrome 中扫码登录。
5. 输入商品 ID 或商品链接，点击“开始获取长图”。

## 运行环境

- Windows
- .NET 8 Desktop Runtime
- Google Chrome

## 注意事项

- 程序不会保存、打印或提交淘宝登录凭据。
- 图片定位只使用商品名称拼接店铺搜索页，不使用商品 ID 作为搜索词。
- 页面操作之间加入随机等待，用于降低连续快速访问导致的误判风险。
