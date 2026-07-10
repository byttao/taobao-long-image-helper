# TaobaoLongImageHelper v1.7.0

发布日期：2026-07-10

## 本次发布

- 修复初始化后端口不可连接的问题。
- 启动新 Chrome 后会等待并确认调试端口真正开放。
- 不同端口使用独立 Chrome 用户资料目录，避免启动参数被已有 Chrome 实例吞掉。

## 影响

- 如果 Chrome 没有成功开放调试端口，界面会直接报错，不再误提示初始化成功。
- 重新初始化时更容易判断当前端口是否真的可用。

## 运行环境

- Windows
- .NET 8 Desktop Runtime
- Google Chrome
