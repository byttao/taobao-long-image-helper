# TaobaoLongImageHelper v1.5.0

发布日期：2026-07-10

## 本次发布

- 修正商品标题提取规则。
- 优先读取第一个 `span[class^="mainTitle"]` 元素。
- 标题字段读取顺序：`title` 属性、`value` 属性、文本内容。

## 影响

- 对淘宝商品页新版标题结构更稳定。
- 其他提取流程不变。

## 运行环境

- Windows
- .NET 8 Desktop Runtime
- Google Chrome
