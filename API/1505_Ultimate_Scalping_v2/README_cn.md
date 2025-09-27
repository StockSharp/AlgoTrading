# Ultimate Scalping v2 策略
[English](README.md) | [Русский](README_ru.md)

该策略结合快慢 EMA 与 VWAP，可选的吞没形态和成交量激增过滤器帮助确认信号。仓位使用基于 ATR 的止损和止盈，并可在出现反向信号时平仓。

## 细节

- **做多**：快 EMA 上穿慢 EMA 且价格在 VWAP 之上。
- **做空**：快 EMA 下穿慢 EMA 且价格在 VWAP 之下。
- **指标**：EMA、VWAP、ATR、SMA。
- **止损**：ATR 倍数。
