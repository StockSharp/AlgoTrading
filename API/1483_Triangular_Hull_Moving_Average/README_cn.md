# 三角Hull均线
[English](README.md) | [Русский](README_ru.md)

该策略比较Hull均线与其两根K线前的值，上穿视为做多信号，下穿视为做空信号，可选择仅做多或仅做空模式。

## 详情
- **入场条件**: HMA 与其滞后两根K线值的交叉.
- **多空方向**: 可配置.
- **退出条件**: 反向信号或方向过滤.
- **止损**: 无.
- **默认值**:
  - `Length` = 40
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `EntryMode` = EntryDirection.LongAndShort
- **过滤器**:
  - 类型: 趋势
  - 方向: 可配置
  - 指标: MA
  - 止损: 否
  - 复杂度: 基础
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
