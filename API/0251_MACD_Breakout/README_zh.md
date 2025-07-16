# MACD 突破策略
[English](README.md) | [Русский](README_ru.md)

本策略关注 MACD 指标的突然扩张。当数值明显超出常态范围时，价格往往开始新的走势。

当指标突破根据近期数据及偏差倍数构建的通道时开仓，可做多也可做空，并配合止损。MACD 回到均值附近后平仓。

该系统适合动量交易者捕捉早期突破。
## 详细信息

- **入场条件**: Indicator exceeds average by deviation multiplier.
- **Long/Short**: 双向 directions.
- **退出条件**: Indicator reverts to average.
- **止损**: 是
- **默认值**:
  - `FastEmaPeriod` = 12
  - `SlowEmaPeriod` = 26
  - `SignalPeriod` = 9
  - `SmaPeriod` = 20
  - `DeviationMultiplier` = 2.0m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **筛选条件**:
  - 类别: 突破
  - 方向: 双向
  - 指标: MACD
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 短期
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
