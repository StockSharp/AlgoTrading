# Vwap Macd Strategy
[English](README.md) | [Русский](README_ru.md)

策略结合VWAP和MACD。当价格高于VWAP且MACD在信号线上方时做多；价格低于VWAP且MACD在信号线下方时做空。MACD与信号线反向交叉时离场。

VWAP体现当日价值，MACD交叉揭示动量转变。交易在MACD靠近VWAP时反转发出信号。适合短线动量交易者，ATR规则控制风险。

## 细节
- **入场条件**:
  - 多头: `Close > VWAP && MACD > Signal`
  - 空头: `Close < VWAP && MACD < Signal`
- **多/空**: 双向
- **离场条件**: MACD反向交叉
- **止损**: 百分比止损，使用 `StopLossPercent`
- **默认值**:
  - `MacdFastPeriod` = 12
  - `MacdSlowPeriod` = 26
  - `MacdSignalPeriod` = 9
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **过滤器**:
  - 类别: Mean reversion
  - 方向: 双向
  - 指标: VWAP, MACD
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 中期
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
