# MACD 均值回归策略

该方法关注 MACD 柱状图相对于其平均值的偏离程度。极端的柱状图往往在动能消散后回归，通过监控 MACD 与信号线的差距来捕捉过度的走势。

当柱状图低于均值 `DeviationMultiplier` 倍标准差时做多；当柱状图高于均值同样倍数时做空。柱状图回到平均水平即平仓。

此策略适合愿意反转动量极端的交易者，并以价格百分比止损，防止趋势继续扩展。
## 详细信息
- **入场条件**:
  - **做多**: MACD Histogram < Avg - DeviationMultiplier * StdDev
  - **做空**: MACD Histogram > Avg + DeviationMultiplier * StdDev
- **多空方向**: 双向
- **退出条件**:
  - **做多**: Exit when Histogram > Avg
  - **做空**: Exit when Histogram < Avg
- **止损**: 是
- **默认值**:
  - `FastMacdPeriod` = 12
  - `SlowMacdPeriod` = 26
  - `SignalPeriod` = 9
  - `AveragePeriod` = 20
  - `DeviationMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **筛选条件**:
  - 类别: 均值回归
  - 方向: 双向
  - 指标: MACD
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
