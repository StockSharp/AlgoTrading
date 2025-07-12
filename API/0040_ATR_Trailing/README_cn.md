# ATR跟踪止损 (ATR Trailing Stops)

以ATR倍数跟随价格设置止损。

价格穿越均线时入场, 止损随波动率移动。

## 详情

- **入场条件**: Price above or below MA.
- **多空方向**: Both directions.
- **出场条件**: Trailing stop hit or price crosses MA.
- **止损**: Yes.
- **默认值**:
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 3.0m
  - `MAPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Trend
  - 方向: Both
  - 指标: ATR, MA
  - 止损: Yes
  - 复杂度: Intermediate
  - 时间框架: Intraday
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险等级: Medium
