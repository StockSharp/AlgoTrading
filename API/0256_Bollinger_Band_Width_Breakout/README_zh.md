# Bollinger 带宽突破策略

该策略跟踪布林带宽度的迅速扩张。当数值超出常态时，价格往往开始新的波动。

当带宽突破根据历史数据与偏差倍数设定的区间时开仓，可做多也可做空，并配合止损。带宽回到平均水平即平仓。

适合动量交易者捕捉波动初期的扩张。
## 详细信息

- **入场条件**: Indicator exceeds average by deviation multiplier.
- **Long/Short**: 双向 directions.
- **退出条件**: Indicator reverts to average.
- **止损**: 是
- **默认值**:
  - `BollingerLength` = 20
  - `BollingerDeviation` = 2.0m
  - `AvgPeriod` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StopMultiplier` = 2
- **筛选条件**:
  - 类别: 突破
  - 方向: 双向
  - 指标: Bollinger
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 短期
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
