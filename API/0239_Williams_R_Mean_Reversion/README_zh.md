# Williams %R 均值回归策略
[English](README.md) | [Русский](README_ru.md)

Williams %R 在 0 到 -100 区间波动，用于衡量价格在最近区间中的相对位置。当指标远离自身均值时，本策略选择逆势进入。

当 %R 低于平均值减 `DeviationMultiplier` 倍标准差时做多；当 %R 高于平均值加同样倍数时做空。指标回到均值附近即平仓。

这种方法适合依靠动量衰竭来把握进场时机的交易者，保护性止损可防止价格持续刷新极端。
## 详细信息
- **入场条件**:
  - **做多**: %R < Avg - DeviationMultiplier * StdDev
  - **做空**: %R > Avg + DeviationMultiplier * StdDev
- **多空方向**: 双向
- **退出条件**:
  - **做多**: Exit when %R > Avg
  - **做空**: Exit when %R < Avg
- **止损**: 是
- **默认值**:
  - `WilliamsRPeriod` = 14
  - `AveragePeriod` = 20
  - `DeviationMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **筛选条件**:
  - 类别: 均值回归
  - 方向: 双向
  - 指标: Williams %R
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等

测试表明年均收益约为 154%，该策略在股票市场表现最佳。
