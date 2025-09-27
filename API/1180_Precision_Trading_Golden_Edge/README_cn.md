# Precision Trading Strategy: Golden Edge
[English](README.md) | [Русский](README_ru.md)

该黄金剥头皮策略将快慢 EMA 的交叉与 HMA 的趋势方向结合。只有当 RSI 证实动量且波动性充足时才进行交易。

## 细节

- **入场条件**：
  - **多头**：快速 EMA 上穿慢速 EMA，RSI > 55，HMA 上升，通过波动性过滤。
  - **空头**：快速 EMA 下穿慢速 EMA，RSI < 45，HMA 下降，通过波动性过滤。
- **指标**：EMA、HMA、RSI、ATR、Highest/Lowest。
- **类型**：趋势跟随。
- **周期**：短期。

