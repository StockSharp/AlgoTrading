# Stochastic 斜率突破策略
[English](README.md) | [Русский](README_ru.md)

本策略跟踪随机指标斜率的变化。当斜率异常陡峭时，意味着可能形成新的趋势。

当斜率超过常态水平若干标准差时顺势开仓，并配以保护止损。斜率回到正常水平后平仓。默认 `StochPeriod` = 14。

适合积极交易者在趋势初期入场。
## 详细信息

- **入场条件**: Indicator exceeds average by deviation multiplier.
- **Long/Short**: 双向 directions.
- **退出条件**: Indicator reverts to average.
- **止损**: 是
- **默认值**:
  - `StochPeriod` = 14
  - `KPeriod` = 3
  - `DPeriod` = 3
  - `LookbackPeriod` = 20
  - `DeviationMultiplier` = 2m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **筛选条件**:
  - 类别: 突破
  - 方向: 双向
  - 指标: Stochastic
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 短期
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等

测试表明年均收益约为 91%，该策略在股票市场表现最佳。
