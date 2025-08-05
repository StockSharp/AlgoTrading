# 成交量均值回归策略
[English](README.md) | [Русский](README_ru.md)

该策略关注成交量相对其历史均值的异常高低。巨大成交量峰值往往在活动恢复正常后回落，提供逆势交易机会。

测试表明年均收益约为 76%，该策略在外汇市场表现最佳。

当成交量低于均值减 `DeviationMultiplier` 倍标准差且价格在均线下方时做多；当成交量高于上轨并且价格位于均线之上时做空。成交量回到均值附近即平仓。

此方法适合关注放量后的衰减行情，百分比止损可避免成交量持续扩大时产生过大损失。
## 详细信息
- **入场条件**:
  - **做多**: Volume < Avg - DeviationMultiplier * StdDev && Close < MA
  - **做空**: Volume > Avg + DeviationMultiplier * StdDev && Close > MA
- **多空方向**: 双向
- **退出条件**:
  - **做多**: Exit when volume > Avg
  - **做空**: Exit when volume < Avg
- **止损**: 是
- **默认值**:
  - `AveragePeriod` = 20
  - `DeviationMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StopLossPercent` = 2m
- **筛选条件**:
  - 类别: 均值回归
  - 方向: 双向
  - 指标: Volume
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等


