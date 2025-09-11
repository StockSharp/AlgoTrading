# EMA 5 Alert Candle Short
[English](README.md) | [Русский](README_ru.md)

**EMA 5 Alert Candle Short** 策略等待三根触及 EMA(5) 的蜡烛，然后出现一根未触及 EMA 的蜡烛。当下一根蜡烛跌破该 "警示蜡烛" 的最低价时做空，止盈距离等于止损。

## 详情
- **入场条件**：三根触及 EMA 的蜡烛后，做空突破未触及蜡烛的最低价。
- **多空方向**：仅做空。
- **退出条件**：止损在警示蜡烛最高价，止盈距离相同。
- **止损**：是，基于警示蜡烛区间。
- **默认值**：
  - `EmaPeriod = 5`
  - `RiskPerTrade = 2m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **过滤器**：
  - 分类: 突破
  - 方向: 做空
  - 指标: EMA
  - 止损: 是
  - 复杂度: 基础
  - 时间框架: 日内 (5m)
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
