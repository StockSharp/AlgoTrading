# Multi-Factor Strategy
[English](README.md) | [Русский](README_ru.md)

多因子策略结合 MACD、RSI 和两条移动平均线来确认趋势。当 MACD 线上穿信号线、RSI 低于 70、价格高于 50 周期均线且 50 均线高于 200 均线时做多；反向条件做空。

止损和止盈基于 ATR 的倍数。

## 细节

- **入场条件**：
  - **多头**：`MACD > Signal` && `RSI < 70` && `Close > SMA50` && `SMA50 > SMA200`.
  - **空头**：`MACD < Signal` && `RSI > 30` && `Close < SMA50` && `SMA50 < SMA200`.
- **方向**：双向。
- **出场条件**：ATR 止损与止盈。
- **止损**：是。
- **默认值**：
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `RsiLength` = 14
  - `AtrLength` = 14
  - `StopAtrMultiplier` = 2
  - `ProfitAtrMultiplier` = 3
  - `CandleType` = TimeSpan.FromMinutes(15)
- **过滤器**：
  - 分类：趋势跟随
  - 方向：双向
  - 指标：MACD, RSI, SMA, ATR
  - 止损：是
  - 复杂度：基础
  - 时间框架：中期
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
