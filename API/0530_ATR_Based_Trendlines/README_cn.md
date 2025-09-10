# 基于ATR的趋势线
[English](README.md) | [Русский](README_ru.md)

该策略从枢轴点构建基于ATR的趋势线，并在价格突破时开仓。

## 详情

- **入场条件**: 突破ATR趋势线。
- **多空方向**: 双向。
- **退出条件**: 反向突破。
- **止损**: 无。
- **默认值**:
  - `LookbackLength` = 30
  - `AtrPercent` = 1.0
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**:
  - 类型: 趋势
  - 方向: 双向
  - 指标: ATR, Price Action
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 日内 (1m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
