# Trend Magic with EMA, SMA, and Auto-Trading Strategy
[English](README.md) | [Русский](README_ru.md)

该策略将基于 CCI 的 Trend Magic 线与 EMA(45)、SMA(90) 和 SMA(180) 结合使用。当 Trend Magic 在多头排列的均线上变为蓝色时做多；当其在空头排列时变为红色则做空。每笔交易在进入时固定止损于 SMA90，止盈按固定的风险回报比计算。

## 细节

- **入场条件**：
  - **多头**：`EMA45 > SMA90 > SMA180` 且 Trend Magic 变为蓝色。
  - **空头**：`EMA45 < SMA90 < SMA180` 且 Trend Magic 变为红色。
- **出场**：止损设置在进入时的 SMA90，止盈为 `entry ± risk * ratio`。
- **止损**：使用止损和止盈。
- **默认值**：
  - `CCI Period` = 21
  - `ATR Period` = 7
  - `ATR Multiplier` = 1.0
  - `Risk Reward` = 1.5
