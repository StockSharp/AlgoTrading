# EMA 斜率突破策略
[English](README.md) | [Русский](README_ru.md)

本策略监测指数移动平均线斜率的变化。斜率异常陡峭时可能预示着新趋势的产生。

当斜率超过常态水平若干标准差时顺势开仓，并设置保护性止损。斜率回归正常后平仓。默认 `EmaLength` = 20。

适合希望提前把握趋势的交易者。
## 详细信息

- **入场条件**: Indicator exceeds average by deviation multiplier.
- **Long/Short**: 双向 directions.
- **退出条件**: Indicator reverts to average.
- **止损**: 是
- **默认值**:
  - `EmaLength` = 20
  - `LookbackPeriod` = 20
  - `DeviationMultiplier` = 2m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **筛选条件**:
  - 类别: 突破
  - 方向: 双向
  - 指标: EMA
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 短期
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等

测试表明年均收益约为 127%，该策略在股票市场表现最佳。

测试表明年均收益约为 79%，该策略在股票市场表现最佳。
