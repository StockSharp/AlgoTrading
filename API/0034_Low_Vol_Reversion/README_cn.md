# 低波动均值回归 (Low Volatility Reversion)

仅在市场安静时启用, 当波动率低于平均水平且价格偏离均线时介入。

目标是在平静环境下捕捉回归, 止损由ATR决定。

## 详情

- **入场条件**: Price away from moving average while ATR is below threshold.
- **多空方向**: Both directions.
- **出场条件**: Price returns to MA or stop triggers.
- **止损**: Yes.
- **默认值**:
  - `MAPeriod` = 20
  - `AtrPeriod` = 14
  - `AtrLookbackPeriod` = 20
  - `AtrThresholdPercent` = 50m
  - `AtrMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Mean Reversion
  - 方向: Both
  - 指标: ATR, MA
  - 止损: Yes
  - 复杂度: Intermediate
  - 时间框架: Intraday
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险等级: Medium
