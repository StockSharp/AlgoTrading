# PercentX Trend Follower
[English](README.md) | [Русский](README_ru.md)

基于 Trendoscope 的 PercentX Trend Follower 策略。

该策略通过选择的通道（Keltner 或 Bollinger）计算价格与中线的距离，构建振荡器，当振荡器突破动态区间时开仓，ATR 用于止损。

## 细节

- **入场条件**：振荡器上穿上区间做多，下破下区间做空。
- **多空方向**：双向。
- **出场条件**：基于 ATR 的止损。
- **止损**：初始 ATR 止损。
- **默认值**：
  - `BandType` = Keltner
  - `MaLength` = 40
  - `LoopbackPeriod` = 80
  - `OuterLoopback` = 80
  - `UseInitialStop` = true
  - `AtrLength` = 14
  - `TrendMultiplier` = 1
  - `ReverseMultiplier` = 3
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**：
  - 分类: Trend
  - 方向: Both
  - 指标: BollingerBands, KeltnerChannels, ATR, Highest, Lowest
  - 止损: ATR
  - 复杂度: 中等
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中等

