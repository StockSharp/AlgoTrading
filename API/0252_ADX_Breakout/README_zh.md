# ADX 突破策略

该策略监控 ADX 指标的迅速上升。当读数明显超出常态时，价格往往开始新的趋势。

当指标突破由近期数据和偏差倍数形成的通道时开仓，可做多亦可做空，并设置止损。ADX 回到均值附近即平仓。

适合动量交易者捕捉早期突破机会。
## 详细信息

- **入场条件**: Indicator exceeds average by deviation multiplier.
- **Long/Short**: 双向 directions.
- **退出条件**: Indicator reverts to average.
- **止损**: 是
- **默认值**:
  - `ADXPeriod` = 14
  - `AvgPeriod` = 20
  - `Multiplier` = 0.1m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StopLoss` = 2.0m
- **筛选条件**:
  - 类别: 突破
  - 方向: 双向
  - 指标: ADX
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 短期
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
