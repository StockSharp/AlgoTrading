# CCI 均值回归策略
[English](README.md) | [Русский](README_ru.md)

商品通道指数(CCI)用来衡量价格偏离其统计平均值的程度。当 CCI 大幅背离均值时，策略预期其在动能减弱后回归。

当 CCI 低于均值减 `DeviationMultiplier` 倍标准差时做多；当 CCI 高于均值加同样倍数时做空。CCI 回到均值附近便平仓。

该系统适合短线逆势交易者，百分比止损可防止市场未能及时回归导致的风险。
## 详细信息
- **入场条件**:
  - **做多**: CCI < Avg - DeviationMultiplier * StdDev
  - **做空**: CCI > Avg + DeviationMultiplier * StdDev
- **多空方向**: 双向
- **退出条件**:
  - **做多**: Exit when CCI > Avg
  - **做空**: Exit when CCI < Avg
- **止损**: 是
- **默认值**:
  - `CciPeriod` = 20
  - `AveragePeriod` = 20
  - `DeviationMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **筛选条件**:
  - 类别: 均值回归
  - 方向: 双向
  - 指标: CCI
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
