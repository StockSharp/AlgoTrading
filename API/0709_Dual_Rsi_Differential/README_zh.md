# Dual RSI Differential
[English](README.md) | [Русский](README_ru.md)

Dual RSI Differential 使用两个 RSI 周期的差值来判断行情，
当差值超过预设阈值时开仓，捕捉短期与长期动量的分歧。

## 细节
- **数据**: 价格K线。
- **入场条件**:
  - **多头**: `RSI(Long) - RSI(Short)` < `RsiDiffLevel`。
  - **空头**: `RSI(Long) - RSI(Short)` > `RsiDiffLevel`。
- **离场条件**: 反向阈值、可选持仓天数、可选止盈/止损。
- **止损**: 可选的止盈和止损 (`Condition`)。
- **默认参数**:
  - `ShortRsiPeriod` = 21
  - `LongRsiPeriod` = 42
  - `RsiDiffLevel` = 5
  - `UseHoldDays` = True
  - `HoldDays` = 5
  - `Condition` = None
  - `TakeProfitPerc` = 15
  - `StopLossPerc` = 10
- **过滤器**:
  - 类型: 动量
  - 方向: 多空皆可
  - 指标: RSI
  - 复杂度: 基础
  - 风险级别: 中等
