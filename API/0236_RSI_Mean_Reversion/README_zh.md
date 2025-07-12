# RSI 均值回归策略

该策略跟踪相对强弱指数(RSI)与其平均值的偏离程度。当 RSI 偏离超过近期标准差的数倍时，预期其向均值回归。

当 RSI 低于平均值减 `Multiplier` 倍标准差时做多；当 RSI 高于平均值加同样倍数时做空。RSI 回到移动平均附近即平仓。

此方法适合寻找超买超卖信号的交易者，波动率带可以根据市况调整阈值，止损有助于控制风险。
## 详细信息
- **入场条件**:
  - **做多**: RSI < Avg - Multiplier * StdDev
  - **做空**: RSI > Avg + Multiplier * StdDev
- **多空方向**: 双向
- **退出条件**:
  - **做多**: Exit when RSI > Avg
  - **做空**: Exit when RSI < Avg
- **止损**: 是
- **默认值**:
  - `RsiPeriod` = 14
  - `AveragePeriod` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **筛选条件**:
  - 类别: 均值回归
  - 方向: 双向
  - 指标: RSI
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
