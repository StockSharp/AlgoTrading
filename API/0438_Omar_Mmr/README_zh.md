# Omar MMR 策略
[English](README.md) | [Русский](README_ru.md)

该动量策略融合了 RSI、三条 EMA 以及 MACD 金叉。仅在价格高于慢 EMA、快 EMA 高于中 EMA、MACD 向上交叉且 RSI 位于29‑70之间时做多。

止盈和止损百分比通过系统的保护模块设置。该方法强调动量与趋势的统一，避免 RSI 过度延伸。

## 细节

- **入场条件**：
  - **多头**：收盘价高于 EMA C，EMA A > EMA B，MACD 线上穿信号线，RSI 在29–70之间。
- **出场条件**：
  - 通过预设的止盈或止损退出。
- **指标**：
  - RSI（周期14）
  - EMA A/B/C（周期20/50/200）
  - MACD（12,26,9）
- **止损**：默认止盈1.5%，止损2%。
- **默认值**：
  - `RsiLength` = 14
  - `EmaALength` = 20
  - `EmaBLength` = 50
  - `EmaCLength` = 200
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `TakeProfitPercent` = 1.5
  - `StopLossPercent` = 2.0
- **过滤**：
  - 趋势延续
  - 单一时间框架
  - 指标：RSI、EMA、MACD
  - 止损：是
  - 复杂度：中等
