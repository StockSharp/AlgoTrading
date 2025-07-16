# 双底形态 (Double Bottom Pattern)
[English](README.md) | [Русский](README_ru.md)

侦测两次相近低点形成的反转形态。

第二个低点后出现看涨蜡烛确认后买入。

## 详情

- **入场条件**: Two bottoms form within `SimilarityPercent` after `Distance` bars.
- **多空方向**: Long only.
- **出场条件**: Price fails or stop-loss.
- **止损**: Yes.
- **默认值**:
  - `Distance` = 5
  - `SimilarityPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(15)
  - `StopLossPercent` = 1.0m
- **过滤器**:
  - 类别: Pattern
  - 方向: Long
  - 指标: Price Action
  - 止损: Yes
  - 复杂度: Intermediate
  - 时间框架: Intraday
  - 季节性: No
  - 神经网络: No
  - 背离: Yes
  - 风险等级: Medium
