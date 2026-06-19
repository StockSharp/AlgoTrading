# 波动收缩形态 (Volatility Contraction Pattern)
[English](README.md) | [Русский](README_ru.md)

寻找一系列逐渐收窄的价格区间, 为爆发行情蓄势。

测试表明年均收益约为 166%，该策略在股票市场表现最佳。

突破区间高点或低点时入场。

## 详情

- **入场条件**: Range contraction then breakout of recent high/low.
- **多空方向**: Both directions.
- **出场条件**: Price crosses MA or stop.
- **止损**: Yes.
- **默认值**:
  - `MAPeriod` = 20
  - `LookbackPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Breakout
  - 方向: Both
  - 指标: Range, MA
  - 止损: Yes
  - 复杂度: Intermediate
  - 时间框架: Intraday
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险等级: Medium

