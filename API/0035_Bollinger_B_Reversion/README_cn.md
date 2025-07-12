# 布林百分比B回归 (Bollinger Percent B Reversion)

利用Percent B指标在价格越界布林带时做回归。

当Percent B超出0-1范围即进场, 达到阈值或止损退出。

## 详情

- **入场条件**: Percent B outside the 0–1 range.
- **多空方向**: Both directions.
- **出场条件**: Percent B crosses `ExitValue` or stop.
- **止损**: Yes.
- **默认值**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `ExitValue` = 0.5m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Mean Reversion
  - 方向: Both
  - 指标: Bollinger Bands
  - 止损: Yes
  - 复杂度: Basic
  - 时间框架: Intraday
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险等级: Medium
