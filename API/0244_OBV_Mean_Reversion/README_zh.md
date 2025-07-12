# OBV 均值回归策略

OBV 指标跟踪累计成交量的流向，以判断买卖力量。本策略在 OBV 与其均值出现显著背离时寻找回归机会。

当 OBV 低于均值减 `Multiplier` 倍标准差且价格在均线下方时买入；当 OBV 高于均值加同样倍数并且价格在均线上方时卖出。OBV 回到均值线附近即平仓。

该策略适合将成交量因素纳入考虑的交易者，百分比止损用于应对成交量持续扩大的情况。
## 详细信息
- **入场条件**:
  - **做多**: OBV < Avg - Multiplier * StdDev && Close < MA
  - **做空**: OBV > Avg + Multiplier * StdDev && Close > MA
- **多空方向**: 双向
- **退出条件**:
  - **做多**: Exit when OBV > Avg
  - **做空**: Exit when OBV < Avg
- **止损**: 是
- **默认值**:
  - `AveragePeriod` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **筛选条件**:
  - 类别: 均值回归
  - 方向: 双向
  - 指标: OBV
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
