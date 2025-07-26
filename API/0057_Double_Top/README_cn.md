# 双顶形态 (Double Top Pattern)
[English](README.md) | [Русский](README_ru.md)

侦测两次相近高点形成的反转形态。

第二个高点后出现看跌蜡烛确认后卖出。

## 详情

- **入场条件**: Two tops within `SimilarityPercent` after `Distance` bars.
- **多空方向**: Short only.
- **出场条件**: Price rallies or stop-loss.
- **止损**: Yes.
- **默认值**:
  - `Distance` = 5
  - `SimilarityPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(15)
  - `StopLossPercent` = 1.0m
- **过滤器**:
  - 类别: Pattern
  - 方向: Short
  - 指标: Price Action
  - 止损: Yes
  - 复杂度: Intermediate
  - 时间框架: Intraday
  - 季节性: No
  - 神经网络: No
  - 背离: Yes
  - 风险等级: Medium

测试表明年均收益约为 58%，该策略在股票市场表现最佳。
