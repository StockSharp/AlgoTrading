# CCI 斜率突破策略
[English](README.md) | [Русский](README_ru.md)

本策略监测 CCI 斜率的变化。当斜率异常陡峭时，通常意味着新趋势正在形成。

测试表明年均收益约为 94%，该策略在股票市场表现最佳。

当斜率超过常态水平若干标准差时顺势进场，并设置保护止损。斜率恢复正常后平仓。默认 `CciPeriod` = 20。

适合积极交易者把握趋势起始阶段。
## 详细信息

- **入场条件**: Indicator exceeds average by deviation multiplier.
- **Long/Short**: 双向 directions.
- **退出条件**: Indicator reverts to average.
- **止损**: 是
- **默认值**:
  - `CciPeriod` = 20
  - `SlopePeriod` = 20
  - `突破Multiplier` = 2.0m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **筛选条件**:
  - 类别: 突破
  - 方向: 双向
  - 指标: CCI
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 短期
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等

