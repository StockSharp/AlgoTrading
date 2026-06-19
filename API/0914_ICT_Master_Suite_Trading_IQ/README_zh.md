# ICT Master Suite Trading IQ 策略
[English](README.md) | [Русский](README_ru.md)

ICT Master Suite 策略交易每日会话的高点和低点突破。当收盘价高于当日高点时做多，收盘价低于当日低点时做空。持仓使用基于 ATR 的跟踪止损管理。

## 详情

- **入场条件**：
  - 收盘价高于当前会话高点（做多）。
  - 收盘价低于当前会话低点（做空）。
- **多空**：多空双向。
- **出场条件**：
  - 基于 ATR 的跟踪止损。
- **止损**：ATR 跟踪止损。
- **默认参数**：
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 1.5
  - `AllowLong` = true
  - `AllowShort` = true
- **过滤器**：
  - 分类：突破
  - 方向：双向
  - 指标：ATR
  - 止损：是
  - 复杂度：低
  - 时间框架：日内
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中
