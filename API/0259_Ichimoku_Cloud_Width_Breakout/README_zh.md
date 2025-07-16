# Ichimoku 云宽度突破策略
[English](README.md) | [Русский](README_ru.md)

本策略监测 Ichimoku 云层宽度的快速扩张。当宽度显著超出常态时，价格往往会开始新的走势。

云宽突破依据历史数据和倍数设定的界限时开仓，可多可空，并设置止损。宽度回到平均水平后平仓。

适合动量交易者把握趋势起步阶段。
## 详细信息

- **入场条件**: Indicator exceeds average by deviation multiplier.
- **Long/Short**: 双向 directions.
- **退出条件**: Indicator reverts to average.
- **止损**: 是
- **默认值**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanBPeriod` = 52
  - `AvgPeriod` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StopLoss` = 2.0m
- **筛选条件**:
  - 类别: 突破
  - 方向: 双向
  - 指标: Ichimoku
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 短期
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
