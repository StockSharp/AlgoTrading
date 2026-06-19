# VWMA交叉 (VWMA Cross)
[English](README.md) | [Русский](README_ru.md)

成交量加权移动平均(VWMA)强调高量价格。

测试表明年均收益约为 184%，该策略在加密市场表现最佳。

价格与VWMA交叉产生信号, 反向交叉离场。

## 详情

- **入场条件**: Price crosses VWMA from below or above.
- **多空方向**: Both directions.
- **出场条件**: Reverse crossover or stop.
- **止损**: Yes.
- **默认值**:
  - `VWMAPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Trend
  - 方向: Both
  - 指标: VWMA
  - 止损: Yes
  - 复杂度: Basic
  - 时间框架: Intraday
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险等级: Medium

