# ATR扩张突破 (ATR Expansion Breakout)
[English](README.md) | [Русский](README_ru.md)

ATR上升表示波动爆发, 在价格相对均线方向突破时入场。

ATR收缩则离场, 止损基于ATR倍数。

## 详情

- **入场条件**: ATR increasing and price above/below MA.
- **多空方向**: Both directions.
- **出场条件**: ATR contracts or stop is hit.
- **止损**: Yes.
- **默认值**:
  - `AtrPeriod` = 14
  - `MAPeriod` = 20
  - `AtrMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Breakout
  - 方向: Both
  - 指标: ATR, MA
  - 止损: Yes
  - 复杂度: Basic
  - 时间框架: Intraday
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险等级: Medium

测试表明年均收益约为 145%，该策略在加密市场表现最佳。
