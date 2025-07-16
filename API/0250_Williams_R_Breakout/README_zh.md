# Williams %R 突破策略
[English](README.md) | [Русский](README_ru.md)

本策略通过观察 Williams %R 相对其历史均值的强势突破来捕捉动量。当指标远离常态区间时，往往预示着趋势即将展开。

当 %R 高于均值加 `Multiplier` 倍标准差时做多；当 %R 低于均值减同样倍数时做空。指标回到均值附近或触及止损后平仓。

该方法适合追求早期入场的突破交易者，仓位风险通过按入场价百分比计算的止损控制。
## 详细信息
- **入场条件**:
  - **做多**: %R > Avg + Multiplier * StdDev
  - **做空**: %R < Avg - Multiplier * StdDev
- **多空方向**: 双向
- **退出条件**:
  - **做多**: Exit when %R < Avg
  - **做空**: Exit when %R > Avg
- **止损**: 是
- **默认值**:
  - `WilliamsRPeriod` = 14
  - `AvgPeriod` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **筛选条件**:
  - 类别: 突破
  - 方向: 双向
  - 指标: Williams %R
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
