# ADX 均值回归策略

平均趋向指数(ADX)用于衡量市场趋势力度。当 ADX 值处于低位时，市场倾向于围绕均值震荡。本策略交易 ADX 相对其均值的偏离。

当 ADX 低于平均值减 `DeviationMultiplier` 倍标准差且价格位于均线下方时做多；当 ADX 高于上轨并且价格在均线上方时做空。ADX 回归均值附近后平仓。

此系统适合在弱趋势环境中寻找机会的交易者，若出现新的趋势则由止损限制亏损。
## 详细信息
- **入场条件**:
  - **做多**: ADX < Avg - DeviationMultiplier * StdDev && Close < MA
  - **做空**: ADX > Avg + DeviationMultiplier * StdDev && Close > MA
- **多空方向**: 双向
- **退出条件**:
  - **做多**: Exit when ADX > Avg
  - **做空**: Exit when ADX < Avg
- **止损**: 是
- **默认值**:
  - `AdxPeriod` = 14
  - `AveragePeriod` = 20
  - `DeviationMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **筛选条件**:
  - 类别: 均值回归
  - 方向: 双向
  - 指标: ADX
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
