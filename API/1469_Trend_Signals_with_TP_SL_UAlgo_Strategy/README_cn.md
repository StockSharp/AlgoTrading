# Trend Signals with TP & SL Strategy
[English](README.md) | [Русский](README_ru.md)

该策略利用基于 ATR 的通道来判断趋势方向。当价格突破上轨时开始新的上升趋势并开多；跌破下轨时开始下降趋势并开空。每笔交易的止损和止盈均通过 ATR 倍数计算。

## 细节

- **入场条件**：
  - **多头**：趋势转为向上。
  - **空头**：趋势转为向下。
- **出场**：止损 `entry ∓ ATR * SL`，止盈 `entry ± ATR * TP`。
- **止损**：同时设置止损和止盈。
- **默认值**：
  - `Sensitivity` = 2
  - `ATR Length` = 14
  - `ATR TP Multiplier` = 2
  - `ATR SL Multiplier` = 1
