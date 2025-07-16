# Stochastic 均值回归策略
[English](README.md) | [Русский](README_ru.md)

该策略将随机指标 %K 与其平均值比较，寻找过度波动的情形。当 %K 偏离均值数个标准差时，预期其回落。

当 %K 低于平均值减 `Multiplier` 倍标准差时买入；当 %K 高于平均值加同倍数时卖出。%K 回到均值线附近即平仓。

此方法适合短线交易者捕捉超买或超卖的极端，止损可避免持续的动量造成亏损。
## 详细信息
- **入场条件**:
  - **做多**: %K < Avg - Multiplier * StdDev
  - **做空**: %K > Avg + Multiplier * StdDev
- **多空方向**: 双向
- **退出条件**:
  - **做多**: Exit when %K > Avg
  - **做空**: Exit when %K < Avg
- **止损**: 是
- **默认值**:
  - `StochPeriod` = 14
  - `KPeriod` = 3
  - `DPeriod` = 3
  - `AveragePeriod` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **筛选条件**:
  - 类别: 均值回归
  - 方向: 双向
  - 指标: Stochastic Oscillator
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
