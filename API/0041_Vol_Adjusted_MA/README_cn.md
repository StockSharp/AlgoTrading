# 波动率调节均线 (Volatility Adjusted Moving Average)

在均线上下添加ATR倍数带。

价格突破带上缘做多, 突破下缘做空, 回到均线平仓。

## 详情

- **入场条件**: Price breaks above or below MA ± ATR multiplier.
- **多空方向**: Both directions.
- **出场条件**: Price crosses MA or stop.
- **止损**: Yes.
- **默认值**:
  - `MAPeriod` = 20
  - `ATRPeriod` = 14
  - `ATRMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Trend
  - 方向: Both
  - 指标: MA, ATR
  - 止损: Yes
  - 复杂度: Intermediate
  - 时间框架: Intraday
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险等级: Medium
