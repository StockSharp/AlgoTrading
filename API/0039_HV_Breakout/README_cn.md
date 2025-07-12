# 历史波动率突破 (Historical Volatility Breakout)

利用历史波动率设定动态阈值, 突破该阈值表明新趋势。

根据标准差和均线生成的水平进行交易。

## 详情

- **入场条件**: Price breaks above or below HV-based level.
- **多空方向**: Both directions.
- **出场条件**: Price crosses MA or stop.
- **止损**: Yes.
- **默认值**:
  - `HvPeriod` = 20
  - `MAPeriod` = 20
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Breakout
  - 方向: Both
  - 指标: HV, MA
  - 止损: Yes
  - 复杂度: Intermediate
  - 时间框架: Intraday
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险等级: Medium
