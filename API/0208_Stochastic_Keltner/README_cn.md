# Stochastic Keltner Strategy
[English](README.md) | [Русский](README_ru.md)

该策略结合随机指标与Keltner通道。当随机%K小于20且价格位于下轨时做多；当随机%K大于80且价格在上轨时做空。

适合在混合市场中寻找机会的交易者。

## 细节
- **入场条件**:
  - 多头: `Stoch %K < 20 && Price < Keltner lower band`
  - 空头: `Stoch %K > 80 && Price > Keltner upper band`
- **多/空**: 双向
- **离场条件**:
  - 多头: 价格回到中轨时平仓
  - 空头: 价格回到中轨时平仓
- **止损**: 是
- **默认值**:
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochD` = 3
  - `EmaPeriod` = 20
  - `KeltnerMultiplier` = 2m
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Mixed
  - 方向: 双向
  - 指标: Stochastic Keltner
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
