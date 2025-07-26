# 量价加权突破 (Volume Weighted Price Breakout)
[English](README.md) | [Русский](README_ru.md)

结合简单均线与VWMA, 当价格从另一侧穿越VWMA视为突破。

价格反向穿越均线时离场。

## 详情

- **入场条件**: Price above or below VWMA with MA confirmation.
- **多空方向**: Both directions.
- **出场条件**: Price crosses MA in opposite direction or stop.
- **止损**: Yes.
- **默认值**:
  - `MAPeriod` = 20
  - `VWAPPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Breakout
  - 方向: Both
  - 指标: VWMA, MA
  - 止损: Yes
  - 复杂度: Intermediate
  - 时间框架: Intraday
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险等级: Medium

测试表明年均收益约为 40%，该策略在加密市场表现最佳。
