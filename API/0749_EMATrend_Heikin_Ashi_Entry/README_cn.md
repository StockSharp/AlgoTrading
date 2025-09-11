# EMA Trend Heikin Ashi Entry 策略
[English](README.md) | [Русский](README_ru.md)

该策略在 Heikin Ashi 蜡烛图上应用布林带，并结合高时间框架的 EMA 趋势过滤器。当高时间框架的快速 EMA 高于慢速 EMA 时，若连续多个 Heikin Ashi 熊蜡烛触及下轨后出现一根上涨蜡烛收于下轨之上则做多；反之做空。

入场后，设置与风险相等的第一目标位，并根据上一根蜡烛的极值移动止损。

## 细节

- **入场条件**：
  - 多头：至少两根熊 HA 蜡烛触及下轨，之后一根牛蜡烛收于其上且高时间框架快速 EMA > 慢速 EMA
  - 空头：至少两根牛 HA 蜡烛触及上轨，之后一根熊蜡烛收于其下且高时间框架快速 EMA < 慢速 EMA
- **多空**：双向
- **出场条件**：
  - 多头：第一目标 1R，之后止损跟随前一根蜡烛低点
  - 空头：第一目标 1R，之后止损跟随前一根蜡烛高点
- **止损**：上一根蜡烛的低/高点
- **默认值**：
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2m
  - `FastEmaPeriod` = 9
  - `SlowEmaPeriod` = 21
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `HigherTimeframe` = TimeSpan.FromMinutes(180).TimeFrame()
- **筛选**：
  - 类别：回调
  - 方向：双向
  - 指标：Bollinger Bands, Heikin Ashi, EMA
  - 止损：是
  - 复杂度：中等
  - 时间框架：中期
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
