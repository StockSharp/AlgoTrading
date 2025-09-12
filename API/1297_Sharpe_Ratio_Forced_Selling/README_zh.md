# Sharpe Ratio Forced Selling
[English](README.md) | [Русский](README_ru.md)

Sharpe Ratio Forced Selling 策略在滚动夏普比率低于负阈值时做多，当夏普比率高于正阈值或持仓时间超过限制时退出。收益可使用对数或简单百分比计算，并调整无风险收益率。

## 详细信息

- **入场条件**: 夏普比率低于 `EntrySharpeThreshold`.
- **多空**: 仅做多.
- **出场条件**: 夏普比率高于 `ExitSharpeThreshold` 或超过 `MaxHoldingDays`.
- **止损**: 无.
- **默认值**:
  - `Length` = 8
  - `EntrySharpeThreshold` = -5
  - `ExitSharpeThreshold` = 13
  - `MaxHoldingDays` = 80
  - `UseLogReturns` = true
  - `RiskFreeRateAnnual` = 0
  - `PeriodsPerYear` = 252
- **过滤器**:
  - 分类: Mean Reversion
  - 方向: Long
  - 指标: Sharpe Ratio
  - 止损: No
  - 复杂度: Intermediate
  - 时间框架: Medium-term
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险级别: Medium
