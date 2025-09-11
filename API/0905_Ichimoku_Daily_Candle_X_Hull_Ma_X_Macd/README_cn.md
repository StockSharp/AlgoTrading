# Ichimoku Daily Candle X Hull MA X MACD 策略
[English](README.md) | [Русский](README_ru.md)

该策略结合 Ichimoku 领先线、日线方向、Hull 均线趋势以及基于 HMA 的 MACD。当所有条件看涨时做多，当所有条件看跌时做空。

## 详情

- **入场条件**：
  - **多头**：HMA 上升，当前价格高于前一根 HMA，当前日线高于上一日，SenkouA > SenkouB，MACD 线 > 信号线。
  - **空头**：HMA 下降，价格低于前一根 HMA，当前日线低于上一日，SenkouA < SenkouB，MACD 线 < 信号线。
- **多空方向**：双向。
- **退出条件**：反向信号。
- **止损**：无。
- **默认值**：
  - `HmaPeriod` = 14
  - `ConversionPeriod` = 9
  - `BasePeriod` = 26
  - `SpanPeriod` = 52
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `PriceSource` = Open
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **过滤器**：
  - 类别：趋势跟随
  - 方向：双向
  - 指标：Ichimoku、Hull MA、MACD
  - 止损：无
  - 复杂度：中等
  - 时间框架：日内
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
