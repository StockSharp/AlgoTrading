# Parabolic SAR 距离突破策略
[English](README.md) | [Русский](README_ru.md)

本策略监控 Parabolic SAR 指标的迅速扩张。当读数突破近期区间时，价格通常会展开新走势。

测试表明年均收益约为 118%，该策略在股票市场表现最佳。

当指标突破根据近期数据和偏差倍数构成的带宽时开仓，可做多或做空，并设置止损。Parabolic SAR 回到均值附近后平仓。

适合动量交易者提前捕捉突破行情。
## 详细信息

- **入场条件**: Indicator exceeds average by deviation multiplier.
- **Long/Short**: 双向 directions.
- **退出条件**: Indicator reverts to average.
- **止损**: 是
- **默认值**:
  - `Acceleration` = 0.02m
  - `MaxAcceleration` = 0.2m
  - `LookbackPeriod` = 20
  - `DeviationMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **筛选条件**:
  - 类别: 突破
  - 方向: 双向
  - 指标: Parabolic
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 短期
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等


