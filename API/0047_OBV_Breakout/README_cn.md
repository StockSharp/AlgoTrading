# OBV突破 (OBV Breakout)

观察能量潮(OBV)突破历史高点或低点并由价格确认。

跟随OBV方向交易。

## 详情

- **入场条件**: OBV surpasses highest or lowest value in lookback period.
- **多空方向**: Both directions.
- **出场条件**: OBV crosses its MA or stop.
- **止损**: Yes.
- **默认值**:
  - `LookbackPeriod` = 20
  - `OBVMAPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Breakout
  - 方向: Both
  - 指标: OBV, MA
  - 止损: Yes
  - 复杂度: Intermediate
  - 时间框架: Intraday
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险等级: Medium
