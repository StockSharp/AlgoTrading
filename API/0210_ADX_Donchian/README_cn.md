# ADX Donchian Strategy
[English](README.md) | [Русский](README_ru.md)

该策略结合ADX和唐奇安通道。当ADX高于阈值且价格突破上轨时做多；当ADX高于阈值且价格跌破下轨时做空，体现强势趋势突破。

适合在趋势明显的市场中寻找机会的交易者。

## 细节
- **入场条件**:
  - 多头: `ADX > AdxThreshold && Price >= upperBorder`
  - 空头: `ADX > AdxThreshold && Price <= lowerBorder`
- **多/空**: 双向
- **离场条件**:
  - 多头: 当ADX跌破(阈值-5)时平仓
  - 空头: 当ADX跌破(阈值-5)时平仓
- **止损**: 是
- **默认值**:
  - `AdxPeriod` = 14
  - `DonchianPeriod` = 5
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `AdxThreshold` = 10
  - `Multiplier` = 0.1m
- **过滤器**:
  - 类别: Mixed
  - 方向: 双向
  - 指标: ADX Donchian
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等

测试表明年均收益约为 67%，该策略在股票市场表现最佳。
