# Bollinger Bands Modified 策略
[English](README.md) | [Русский](README_ru.md)

该策略利用布林带突破并可选用 EMA 趋势过滤器。当价格上穿上轨时做多，下穿下轨时做空。

止损放在最近的高点或低点，止盈为风险的倍数。

## 细节

- **入场条件**：
  - 多头：价格上穿布林带上轨
  - 空头：价格下穿布林带下轨
- **方向**：多空双向
- **出场条件**：
  - 多头：止损为最近低点，目标=风险*系数
  - 空头：止损为最近高点，目标=风险*系数
- **止损**：最近 N 根蜡烛的最高/最低
- **默认值**：
  - `BollingerLength` = 20
  - `BollingerDeviation` = 0.38m
  - `EmaLength` = 80
  - `HighestLength` = 7
  - `LowestLength` = 7
  - `TargetFactor` = 1.6m
  - `EmaTrend` = true
  - `CrossoverCheck` = false
  - `CrossunderCheck` = false
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **筛选**：
  - 类型：趋势
  - 方向：双向
  - 指标：Bollinger Bands, EMA, Highest, Lowest
  - 止损：有
  - 复杂度：入门
  - 时间框架：中期
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中
