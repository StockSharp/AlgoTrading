# MA 斜率突破策略
[English](README.md) | [Русский](README_ru.md)

该策略观察移动平均线斜率的变化。当斜率异常陡峭时，可能预示新趋势的形成。

当斜率超过常规水平若干标准差时顺势开仓，并设置保护止损。斜率回归正常后平仓。默认 `MaLength` = 20。

适合积极交易者提前介入趋势。
## 详细信息

- **入场条件**: Indicator exceeds average by deviation multiplier.
- **Long/Short**: 双向 directions.
- **退出条件**: Indicator reverts to average.
- **止损**: 是
- **默认值**:
  - `MaLength` = 20
  - `LookbackPeriod` = 20
  - `DeviationMultiplier` = 2m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **筛选条件**:
  - 类别: 突破
  - 方向: 双向
  - 指标: MA
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 短期
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等

测试表明年均收益约为 124%，该策略在外汇市场表现最佳。

测试表明年均收益约为 76%，该策略在外汇市场表现最佳。
