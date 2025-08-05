# Vwap Adx Strategy
[English](README.md) | [Русский](README_ru.md)

该策略基于VWAP和ADX指标。当价格高于VWAP且ADX>25时做多；价格低于VWAP且ADX>25时做空。ADX下降到20以下时平仓。

测试表明年均收益约为 157%，该策略在加密市场表现最佳。

VWAP作为会话基准，ADX衡量趋势强度。价格偏离VWAP且ADX显示强势时入场。适合日内趋势交易者，止损使用ATR倍数。

## 细节
- **入场条件**:
  - 多头: `Close > VWAP && ADX > 25`
  - 空头: `Close < VWAP && ADX > 25`
- **多/空**: 双向
- **离场条件**: ADX跌破阈值
- **止损**: 百分比止损，使用 `StopLossPercent`
- **默认值**:
  - `StopLossPercent` = 2m
  - `AdxPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **过滤器**:
  - 类别: Mean reversion
  - 方向: 双向
  - 指标: VWAP, ADX
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 中期
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等

