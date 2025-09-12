# Improved EMA & CDC Trailing Stop Strategy
[English](README.md) | [Русский](README_ru.md)

该策略结合 EMA 趋势过滤、MACD 确认和基于 ATR 的 CDC 移动止损。

## 详情

- **入场条件**:
  - **多头**: 价格 > EMA60，EMA60 > EMA90，MACD 线 > 信号线。
  - **空头**: 价格 < EMA60，EMA60 < EMA90，MACD 线 < 信号线。
- **多/空**: 双向。
- **出场条件**:
  - 移动止损或基于 ATR 的止盈。
- **止损**: 是。
- **默认值**:
  - `Ema60Period` = 60
  - `Ema90Period` = 90
  - `AtrPeriod` = 24
  - `Multiplier` = 4
  - `ProfitTargetMultiplier` = 2
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **过滤器**:
  - 类别: 趋势跟随
  - 方向: 双向
  - 指标: EMA, MACD, ATR
  - 止损: 是
  - 复杂度: 基础
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
