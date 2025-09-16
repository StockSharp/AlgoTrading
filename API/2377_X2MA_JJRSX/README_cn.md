# X2MA JJRSX 策略
[English](README.md) | [Русский](README_ru.md)

该策略结合双移动平均线趋势过滤器和基于RSI的入场触发。
通过比较快慢均线在较高时间框架上确定趋势。
当RSI在较低时间框架上脱离超卖或超买区域并顺着趋势时执行交易。

## 细节

- **入场条件**：
  - 多头：趋势向上且RSI上穿 `Oversold`
  - 空头：趋势向下且RSI下穿 `Overbought`
- **多/空**：均可
- **出场条件**：相反的RSI阈值或趋势反转
- **止损**：无
- **默认参数**：
  - `TrendCandleType` = 4小时K线
  - `SignalCandleType` = 30分钟K线
  - `FastMaPeriod` = 12
  - `SlowMaPeriod` = 5
  - `RsiPeriod` = 8
  - `Overbought` = 70
  - `Oversold` = 30

