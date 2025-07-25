# RSI 斜率突破策略
[English](README.md) | [Русский](README_ru.md)

本策略监测 RSI 斜率的变化。若斜率异常陡峭，表明可能产生新的趋势。

当斜率超过正常水平若干标准差时顺势进场，并设置保护止损。斜率回归常态后离场。默认 `RsiPeriod` = 14。

适合想要在趋势早期入场的交易者。
## 详细信息

- **入场条件**: Indicator exceeds average by deviation multiplier.
- **Long/Short**: 双向 directions.
- **退出条件**: Indicator reverts to average.
- **止损**: 是
- **默认值**:
  - `RsiPeriod` = 14
  - `LookbackPeriod` = 20
  - `DeviationMultiplier` = 2m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **筛选条件**:
  - 类别: 突破
  - 方向: 双向
  - 指标: RSI
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 短期
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等

测试表明年均收益约为 136%，该策略在股票市场表现最佳。

测试表明年均收益约为 88%，该策略在股票市场表现最佳。
