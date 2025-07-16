# 波动率调整的动量突破策略
[English](README.md) | [Русский](README_ru.md)

该策略监控市场波动率的快速扩张。当读数超出平均范围时，价格往往展开新的趋势。

当指标突破依靠近期数据和偏差倍数设定的通道时开仓，可做多或做空，并带止损。波动率回到均值附近后平仓。

非常适合动量交易者捕捉早期突破。
## 详细信息

- **入场条件**: Indicator exceeds average by deviation multiplier.
- **Long/Short**: 双向 directions.
- **退出条件**: Indicator reverts to average.
- **止损**: 是
- **默认值**:
  - `动量Period` = 14
  - `AtrPeriod` = 14
  - `LookbackPeriod` = 20
  - `DeviationMultiplier` = 2m
  - `StopLoss` = new Unit(2
  - `CandleType` = TimeSpan.FromMinutes(5)
- **筛选条件**:
  - 类别: 突破
  - 方向: 双向
  - 指标: Volatility
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 短期
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
