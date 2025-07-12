# 震荡指数突破 (Choppiness Index Breakout)

利用Choppiness Index判断市场从震荡转向趋势。

指标低于阈值时跟随新趋势进入。

## 详情

- **入场条件**: Choppiness below `ChoppinessThreshold` with price above/below MA.
- **多空方向**: Both directions.
- **出场条件**: Choppiness above `HighChoppinessThreshold` or stop.
- **止损**: Yes.
- **默认值**:
  - `MAPeriod` = 20
  - `ChoppinessPeriod` = 14
  - `ChoppinessThreshold` = 38.2m
  - `HighChoppinessThreshold` = 61.8m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Breakout
  - 方向: Both
  - 指标: Choppiness, MA
  - 止损: Yes
  - 复杂度: Intermediate
  - 时间框架: Intraday
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险等级: Medium
