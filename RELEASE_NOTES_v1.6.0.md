# TaobaoLongImageHelper v1.6.0

发布日期：2026-07-10

## 本次发布

- 初始化逻辑改为优先连接原有 Chrome。
- 如果当前端口已有可连接的 Chrome 调试实例，不再启动新的 Chrome。
- 连接成功后，会在已有 Chrome 中打开淘宝登录页。
- 只有当前端口不可连接时，才自动寻找可用端口并启动新的 Chrome。

## 使用说明

如果之前启动过 Chrome 且未关闭：

1. 重新打开软件。
2. 保持端口为原来的调试端口，例如 `9222`。
3. 点击“初始化”。
4. 程序会连接原有 Chrome 并打开淘宝登录页。

## 运行环境

- Windows
- .NET 8 Desktop Runtime
- Google Chrome
