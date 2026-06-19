# Swing FX Pro Panel v1
[English](README.md) | [Русский](README_ru.md)

基于 EMA 金叉死叉并带有简单统计信息的演示策略。 快速 EMA 向上穿越慢速 EMA 时做多，向下穿越时做空。 每笔交易使用固定的盈利和亏损目标。

## 细节

- **指标**: EMA
- **参数**:
  - `Initial Capital` – 统计用的初始资金。
  - `Risk Per Trade` – 每笔交易风险百分比（信息性）。
  - `Analysis Period` – 分析周期。
  - `Fast Length` – 快速 EMA 周期。
  - `Slow Length` – 慢速 EMA 周期。
  - `Profit Target` – 盈利目标（价格单位）。
  - `Stop Loss` – 止损（价格单位）。

