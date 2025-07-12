# Stochastic 突破策略

本策略监测随机指标 %K 在一段收敛后向上或向下突破经过波动率调整的阈值，以捕捉动能爆发。

当 %K 突破上轨时买入，下破下轨时做空。指标回到均值附近或触发止损则退出。

该策略面向日内交易者，利用波动带过滤噪声，只在明显的突破时进场。
## 详细信息
- **入场条件**:
  - **做多**: %K > Avg + DeviationMultiplier * StdDev
  - **做空**: %K < Avg - DeviationMultiplier * StdDev
- **多空方向**: 双向
- **退出条件**:
  - **做多**: Exit when %K < Avg
  - **做空**: Exit when %K > Avg
- **止损**: 是
- **默认值**:
  - `StochasticPeriod` = 14
  - `KPeriod` = 3
  - `DPeriod` = 3
  - `LookbackPeriod` = 20
  - `DeviationMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **筛选条件**:
  - 类别: 突破
  - 方向: 双向
  - 指标: Stochastic Oscillator
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
