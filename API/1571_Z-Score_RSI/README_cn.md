# Z-Score RSI 策略
[English](README.md) | [Русский](README_ru.md)

该策略在价格的 z-score 上计算 RSI，并使用 RSI 的 EMA 作为信号。当 RSI 上穿其 EMA 时做多，下穿时做空。

## 细节

- **入场条件**: z-score 的 RSI 上穿其 EMA
- **多空**: 双向
- **出场条件**: 反向穿越
- **止损**: 无
- **默认值**:
  - `ZScoreLength` = 20
  - `RsiLength` = 9
  - `SmoothingLength` = 15
- **筛选**:
  - 类别: 振荡器
  - 方向: 双向
  - 指标: SMA, StandardDeviation, RSI, EMA
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 日内
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中等
