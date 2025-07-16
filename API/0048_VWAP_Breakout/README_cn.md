# VWAP突破 (VWAP Breakout)
[English](README.md) | [Русский](README_ru.md)

价格从另一侧穿越成交量加权平均价(VWAP)时视为突破。

回穿VWAP或止损退出。

## 详情

- **入场条件**: Price closes on the opposite side of VWAP.
- **多空方向**: Both directions.
- **出场条件**: Price crosses back through VWAP or stop.
- **止损**: Yes.
- **默认值**:
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Breakout
  - 方向: Both
  - 指标: VWAP
  - 止损: Yes
  - 复杂度: Basic
  - 时间框架: Intraday
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险等级: Medium
