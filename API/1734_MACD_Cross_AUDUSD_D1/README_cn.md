# MACD Cross AUDUSD D1
[English](README.md) | [Русский](README_ru.md)

该策略在日线级别上基于MACD线的交叉交易AUDUSD。

当MACD主线从下向上穿越信号线时开多仓，从上向下穿越时开空仓。交易仅在服务器时间06:00到14:00之间进行，并且同一时间只允许有一个持仓。默认情况下，每笔交易设置40点止损和三倍于止损的止盈。

## 细节

- **入场条件**：MACD主线与信号线交叉。
- **多头/空头**：双向。
- **出场条件**：止损或止盈。
- **止损**：是。
- **默认值**：
  - `Volume` = 0.1
  - `StopLossPips` = 40
  - `RewardRatio` = 3
  - `CandleType` = TimeSpan.FromDays(1)
- **筛选器**：
  - 类别: Trend
  - 方向: Both
  - 指标: MACD
  - 止损: Yes
  - 复杂度: Basic
  - 时间框架: Daily
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险等级: Medium
