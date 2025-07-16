# 成交量背离 (Volume Divergence)
[English](README.md) | [Русский](README_ru.md)

寻找价格走势与成交量不一致的情况。

量增价跌视为吸筹, 量增价涨视为派发, 依此做多或做空。

## 详情

- **入场条件**: Price and volume moving in opposite directions.
- **多空方向**: Both directions.
- **出场条件**: Price crosses MA or stop.
- **止损**: Yes.
- **默认值**:
  - `MAPeriod` = 20
  - `ATRPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Divergence
  - 方向: Both
  - 指标: Volume, MA
  - 止损: Yes
  - 复杂度: Intermediate
  - 时间框架: Intraday
  - 季节性: No
  - 神经网络: No
  - 背离: Yes
  - 风险等级: Medium
