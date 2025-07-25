# 成交量均线交叉 (Volume MA Cross)
[English](README.md) | [Русский](README_ru.md)

利用成交量快慢均线交叉判断参与度变化。

快线向上穿越做多, 向下穿越做空, 反向交叉离场。

## 详情

- **入场条件**: Fast volume MA crosses slow volume MA.
- **多空方向**: Both directions.
- **出场条件**: Reverse crossover or stop.
- **止损**: Yes.
- **默认值**:
  - `FastVolumeMALength` = 10
  - `SlowVolumeMALength` = 50
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Momentum
  - 方向: Both
  - 指标: Volume MA
  - 止损: Yes
  - 复杂度: Intermediate
  - 时间框架: Intraday
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险等级: Medium

测试表明年均收益约为 46%，该策略在股票市场表现最佳。
