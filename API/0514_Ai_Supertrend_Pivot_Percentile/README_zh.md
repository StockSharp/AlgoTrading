# AI Supertrend Pivot Percentile 策略
[English](README.md) | [Русский](README_ru.md)

该策略结合两个 Supertrend 指标、ADX 过滤器以及 Williams %R 枢轴百分位过滤。 当价格位于两个 Supertrend 之上、ADX 显示强趋势且 Williams %R 高于 -50 时开多仓；相反条件下开空仓。

## 详情

- **入场条件**:
  - **多头**: 价格高于两个 Supertrend，ADX 大于阈值，Williams %R > -50。
  - **空头**: 价格低于两个 Supertrend，ADX 大于阈值，Williams %R < -50。
- **多/空**: 双向。
- **离场条件**:
  - 反向信号。
- **止损**: 基于百分比的止盈和止损。
- **默认参数**:
  - `Length1` = 10
  - `Factor1` = 3
  - `Length2` = 20
  - `Factor2` = 4
  - `AdxLength` = 14
  - `AdxThreshold` = 20
  - `PivotLength` = 14
  - `TpPercent` = 2
  - `SlPercent` = 1
- **过滤器**:
  - 类别: 趋势跟随
  - 方向: 双向
  - 指标: SuperTrend, ADX, Williams %R
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 任意
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
