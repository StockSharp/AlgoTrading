# Resampling Reverse Engineering Bands
[English](README.md) | [Русский](README_ru.md)

Resampling Reverse Engineering Bands 利用重采样计算 RSI 的价格带。当价格跌破下轨时做多，价格突破上轨时做空。

## 细节
- **数据**：价格K线。
- **入场条件**：
  - **做多**：收盘价跌破 RRSI 下轨。
  - **做空**：收盘价突破 RRSI 上轨。
- **出场条件**：相反信号。
- **止损**：无。
- **默认值**：
  - `RsiPeriod` = 14
  - `HighThreshold` = 70
  - `LowThreshold` = 30
  - `SampleLength` = 1
- **筛选**：
  - 类别：动量
  - 方向：多空
  - 指标：RSI
  - 复杂度：中等
  - 风险级别：中等
