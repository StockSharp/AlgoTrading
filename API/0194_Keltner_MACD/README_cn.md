# Keltner Macd Strategy
[English](README.md) | [Русский](README_ru.md)

本策略结合Keltner通道和MACD指标。当价格突破上轨且MACD在信号线上方时做多；跌破下轨且MACD在信号线下方时做空。MACD与信号线出现反向交叉时离场。

Keltner通道提供突破信号，MACD动量用于过滤方向。适合寻求波动扩张并依靠动量确认的交易者，止损基于ATR倍数。

## 细节
- **入场条件**:
  - 多头: `Close > UpperBand && MACD > Signal`
  - 空头: `Close < LowerBand && MACD < Signal`
- **多/空**: 双向
- **离场条件**: MACD反向交叉
- **止损**: ATR倍数，使用 `AtrMultiplier`
- **默认值**:
  - `EmaPeriod` = 20
  - `Multiplier` = 2m
  - `AtrPeriod` = 14
  - `MacdFastPeriod` = 12
  - `MacdSlowPeriod` = 26
  - `MacdSignalPeriod` = 9
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **过滤器**:
  - 类别: Mean reversion
  - 方向: 双向
  - 指标: Keltner Channel, MACD
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 中期
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
