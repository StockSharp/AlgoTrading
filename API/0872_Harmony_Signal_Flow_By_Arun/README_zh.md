# Harmony Signal Flow By Arun
[English](README.md) | [Русский](README_ru.md)

Harmony Signal Flow By Arun 使用短周期 RSI 捕捉反转，并设定固定止损和目标。当 RSI 上穿 `LowerThreshold` 时做多，下穿 `UpperThreshold` 时做空。每笔交易在触及止损、目标或每天 15:25 时平仓。

## 细节
- **数据**: 价格K线。
- **入场条件**:
  - **多头**: RSI 上穿 `LowerThreshold`。
  - **空头**: RSI 下穿 `UpperThreshold`。
- **离场条件**: 触发止损或目标，或 15:25 平仓。
- **止损**: 固定止损和目标。
- **默认参数**:
  - `RsiPeriod` = 5
  - `LowerThreshold` = 30
  - `UpperThreshold` = 70
  - `BuyStopLoss` = 100
  - `BuyTarget` = 150
  - `SellStopLoss` = 100
  - `SellTarget` = 150
- **过滤器**:
  - 类型: 均值回归
  - 方向: 多空皆可
  - 指标: RSI
  - 复杂度: 低
  - 风险级别: 中等
