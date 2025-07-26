# Keltner 通道宽度突破策略
[English](README.md) | [Русский](README_ru.md)

本策略观察 Keltner 通道宽度的快速扩张。当指标超出常态范围时，价格往往开始新的走势。

测试表明年均收益约为 112%，该策略在外汇市场表现最佳。

当宽度突破历史数据和偏差倍数设定的界限时进场，既可做多也可做空，并设置止损。宽度回到均值附近则平仓。

适合动量交易者在趋势初期介入。
## 详细信息

- **入场条件**: Indicator exceeds average by deviation multiplier.
- **Long/Short**: 双向 directions.
- **退出条件**: Indicator reverts to average.
- **止损**: 是
- **默认值**:
  - `EMAPeriod` = 20
  - `ATRPeriod` = 14
  - `ATRMultiplier` = 2.0m
  - `AvgPeriod` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StopMultiplier` = 2
- **筛选条件**:
  - 类别: 突破
  - 方向: 双向
  - 指标: Keltner
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 短期
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等


