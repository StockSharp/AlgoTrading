# IU Higher Timeframe MA Cross Strategy
[English](README.md) | [Русский](README_ru.md)

IU Higher Timeframe MA Cross Strategy 在用户指定的高时间框上计算的快速移动平均线与另一条较慢均线发生交叉时入场。向上交叉做多，向下交叉做空。止损放在前一根K线的极值位置，止盈根据可配置的风险回报比计算。

## 详情
- **数据**: 指定时间框的K线。
- **入场条件**:
  - **多头**: MA1 上穿 MA2。
  - **空头**: MA1 下破 MA2。
- **出场条件**: 触及止损或止盈。
- **止损**: 前一根K线的高/低点乘以 `RiskToReward` 计算目标。
- **默认值**:
  - `Ma1CandleType` = 60m
  - `Ma1Length` = 20
  - `Ma1Type` = MovingAverageTypeEnum.Exponential
  - `Ma2CandleType` = 60m
  - `Ma2Length` = 50
  - `Ma2Type` = MovingAverageTypeEnum.Exponential
  - `RiskToReward` = 2
- **过滤器**:
  - 类别: 趋势
  - 方向: 多 & 空
  - 指标: 移动平均线
  - 复杂度: 低
  - 风险水平: 中
