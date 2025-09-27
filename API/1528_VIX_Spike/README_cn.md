# VIX 波动率飙升策略
[English](README.md) | [Русский](README_ru.md)

当 VIX 指数高于其均值加上若干倍标准差时买入，并在固定数量的柱后平仓。

## 详情

- **入场条件**: VIX > 均值 + StdDevMultiplier × 标准差。
- **多空方向**: 仅做多。
- **出场条件**: `ExitPeriods` 根柱后平仓。
- **止损**: 有。
- **默认值**:
  - `StdDevLength` = 15
  - `StdDevMultiplier` = 2
  - `ExitPeriods` = 10
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `VixSecurity` = "CBOE:VIX"
- **过滤器**:
  - 类别: Volatility
  - 方向: Long
  - 指标: SMA, StdDev
  - 止损: Yes
  - 复杂度: Beginner
  - 时间框架: Intraday
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险等级: Medium
