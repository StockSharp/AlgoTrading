# Bollinger Heikin Ashi Entry 策略
[English](README.md) | [Русский](README_ru.md)

该策略在 Heikin Ashi 蜡烛上使用布林带。出现两个连续触及下轨的看跌 HA 蜡烛后，如果下一根 HA 蜡烛收在下轨之上则做多；相反条件做空。

入场后先以风险同等的目标平掉一半仓位，然后用前一根蜡烛的极值追踪止损。

## 细节

- **入场条件**：
  - 多头：两根触及下轨的看跌 HA 蜡烛，然后一根站在下轨之上的看涨蜡烛
  - 空头：两根触及上轨的看涨 HA 蜡烛，然后一根落在上轨之下的看跌蜡烛
- **方向**：多空双向
- **出场条件**：
  - 多头：第一目标=1R，然后按前一根最低价追踪止损
  - 空头：第一目标=1R，然后按前一根最高价追踪止损
- **止损**：上一根蜡烛的极值
- **默认值**：
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **筛选**：
  - 类型：反转
  - 方向：双向
  - 指标：Bollinger Bands, Heikin Ashi
  - 止损：有
  - 复杂度：中等
  - 时间框架：中期
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中
