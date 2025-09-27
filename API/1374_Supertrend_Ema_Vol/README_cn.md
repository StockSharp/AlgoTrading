# Supertrend Ema Vol 策略
[English](README.md) | [Русский](README_ru.md)

该策略结合 Supertrend、EMA 趋势确认以及成交量过滤。当 Supertrend 反转且价格位于 EMA 上下并且成交量超过其 EMA 时入场。使用 ATR 作为止损依据。

## 细节

- **入场条件**：
  - 多头：Supertrend 向上翻转，价格高于 EMA，成交量高于 Volume EMA
  - 空头：Supertrend 向下翻转，价格低于 EMA，成交量高于 Volume EMA
- **多空方向**：可配置
- **出场条件**：Supertrend 反转或 ATR 止损
- **止损**：ATR 倍数
- **默认值**：
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 3m
  - `EmaLength` = 21
  - `StartDate` = new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero)
  - `AllowLong` = true
  - `AllowShort` = false
  - `SlMultiplier` = 2m
  - `UseVolumeFilter` = true
  - `VolumeEmaLength` = 20
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **过滤**：
  - 类别: Trend
  - 方向: Both
  - 指标: Supertrend, EMA, Volume EMA, ATR
  - 止损: ATR
  - 复杂度: Intermediate
  - 时间框架: Intraday
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险等级: Medium
