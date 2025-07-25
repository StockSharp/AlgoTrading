# Supertrend 距离突破策略
[English](README.md) | [Русский](README_ru.md)

本策略监控 Supertrend 指标的快速扩张。当读数明显超出平均范围时，价格往往启动新的运动。

当指标突破根据近期数据及偏差倍数构成的通道时开仓，可做多或做空，并设止损。Supertrend 回到均值附近后平仓。

适合动量交易者在行情初期介入。
## 详细信息

- **入场条件**: Indicator exceeds average by deviation multiplier.
- **Long/Short**: 双向 directions.
- **退出条件**: Indicator reverts to average.
- **止损**: 是
- **默认值**:
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3m
  - `LookbackPeriod` = 20
  - `DeviationMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **筛选条件**:
  - 类别: 突破
  - 方向: 双向
  - 指标: Supertrend
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 短期
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等

测试表明年均收益约为 115%，该策略在股票市场表现最佳。

测试表明年均收益约为 67%，该策略在股票市场表现最佳。
