# Multi-Band Comparison
[English](README.md) | [Русский](README_ru.md)

Multi-Band Comparison 使用 SMA、标准差和价格分位数带。当价格连续 `EntryConfirmBars` 根 K 线收在上分位数减去标准差之上时做多；当价格连续 `ExitConfirmBars` 根 K 线收在该水平之下时平仓。

## 详情
- **数据**：价格 K 线。
- **入场条件**：
  - **多头**：收盘价高于（上分位数 - 标准差）并持续 `EntryConfirmBars` 根 K 线。
- **出场条件**：收盘价低于该水平并持续 `ExitConfirmBars` 根 K 线。
- **止损**：无。
- **默认值**：
  - `Length` = 20
  - `BollingerMultiplier` = 1
  - `UpperQuantile` = 0.95
  - `EntryConfirmBars` = 1
  - `ExitConfirmBars` = 1
- **过滤器**：
  - 类别：统计
  - 方向：多头
  - 指标：SMA, Standard Deviation
  - 复杂度：中
  - 风险等级：中
