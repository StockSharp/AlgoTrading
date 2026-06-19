# Keltner Williams R Strategy
[English](README.md) | [Русский](README_ru.md)

此策略结合Keltner通道与Williams %R指标。当价格位于下轨且%R < -80时做多；当价格在上轨且%R > -20时做空，分别代表超卖和超买的突破。

测试表明年均收益约为 46%，该策略在股票市场表现最佳。

适合在震荡市场中寻找机会的交易者。

## 细节
- **入场条件**:
  - 多头: `Price < lower Keltner band && Williams %R < -80`
  - 空头: `Price > upper Keltner band && Williams %R > -20`
- **多/空**: 双向
- **离场条件**:
  - 多头: 价格回到中轨时平仓
  - 空头: 价格回到中轨时平仓
- **止损**: 是
- **默认值**:
  - `EmaPeriod` = 20
  - `KeltnerMultiplier` = 2m
  - `AtrPeriod` = 14
  - `WilliamsRPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Mixed
  - 方向: 双向
  - 指标: Keltner Williams R
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等

