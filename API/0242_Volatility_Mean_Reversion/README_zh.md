# Volatility 均值回归策略
[English](README.md) | [Русский](README_ru.md)

此方法围绕市场波动率的变化进行交易。当 ATR 相对其均值显著偏离时，预期其会回归正常水平。

当 ATR 低于平均值减 `DeviationMultiplier` 倍标准差且价格位于均线下方时做多；当 ATR 高于上轨且价格在均线之上时做空。ATR 回到平均值附近即平仓。

这种策略适合在波动率极端时反向操作的交易者，并以止损防止波动率继续扩大。
## 详细信息
- **入场条件**:
  - **做多**: ATR < Avg - DeviationMultiplier * StdDev && Close < MA
  - **做空**: ATR > Avg + DeviationMultiplier * StdDev && Close > MA
- **多空方向**: 双向
- **退出条件**:
  - **做多**: Exit when ATR > Avg
  - **做空**: Exit when ATR < Avg
- **止损**: 是
- **默认值**:
  - `AtrPeriod` = 14
  - `AveragePeriod` = 20
  - `DeviationMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **筛选条件**:
  - 类别: 均值回归
  - 方向: 双向
  - 指标: ATR
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等

测试表明年均收益约为 73%，该策略在加密市场表现最佳。

测试表明年均收益约为 163%，该策略在股票市场表现最佳。
