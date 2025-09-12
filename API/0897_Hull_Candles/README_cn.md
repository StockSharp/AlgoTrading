# Hull Candles
[English](README.md) | [Русский](README_ru.md)

Hull Candles 是一种简单的趋势跟随策略，使用基于 OHLC 平均价的 Hull 移动平均线。当 HMA 上升且收盘价高于其 SMA 时做多；当 HMA 下降且收盘价低于其 SMA 时做空。

## 细节
- **数据**：价格K线。
- **入场条件**：
  - **多头**：HMA 上升且收盘价 > SMA。
  - **空头**：HMA 下降且收盘价 < SMA。
- **出场条件**：反向信号。
- **止损**：无。
- **默认值**：
  - `BodyLength` = 10
  - `SmaLength` = 1
- **过滤器**：
  - 类别：趋势跟随
  - 方向：多头 & 空头
  - 指标：HMA, SMA
  - 复杂度：低
  - 风险等级：高
