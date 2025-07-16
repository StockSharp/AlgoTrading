# 累积差分突破 (Cumulative Delta Breakout)
[English](README.md) | [Русский](README_ru.md)

累积买卖量差突破历史区间时入场。

当差分回到零或止损触发时退出。

## 详情

- **入场条件**: Cumulative delta exceeds highest or lowest value in lookback.
- **多空方向**: Both directions.
- **出场条件**: Delta crosses zero or stop.
- **止损**: Yes.
- **默认值**:
  - `LookbackPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Breakout
  - 方向: Both
  - 指标: Cumulative Delta
  - 止损: Yes
  - 复杂度: Intermediate
  - 时间框架: Intraday
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险等级: Medium
