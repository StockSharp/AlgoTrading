# Renko Live Charts Pimped Strategy
[English](README.md) | [Русский](README_ru.md)

该策略构建砖形图并在方向变化时进行交易。可选地根据 ATR 计算砖块大小，使砖形结构能够随市场波动调整。

## 细节

- **入场条件**:
  - **多头**: 看跌砖后出现看涨砖。
  - **空头**: 看涨砖后出现看跌砖。
- **多头/空头**: 两者。
- **出场条件**:
  - 反向信号。
- **止损**: 无。
- **默认值**:
  - `BoxSize` = 10m.
  - `Volume` = 1m.
  - `CalculateBestBoxSize` = false.
  - `AtrPeriod` = 24.
  - `AtrCandleType` = 60m.
  - `UseAtrMa` = true.
  - `AtrMaPeriod` = 120.
- **筛选**:
  - 类别: Trend Following
  - 方向: 双向
  - 指标: Renko, ATR
   - 止损: 无
  - 复杂度: Intermediate
  - 时间框架: Renko
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: Medium
