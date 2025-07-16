# MACD Bollinger Strategy
[English](README.md) | [Русский](README_ru.md)

该策略结合MACD与布林带。当MACD高于信号线且价格在下轨以下时做多；当MACD低于信号线且价格在上轨以上时做空。

适合在震荡市场中寻找机会的交易者。

## 细节
- **入场条件**:
  - 多头: `MACD > Signal && Price < BB_lower`
  - 空头: `MACD < Signal && Price > BB_upper`
- **多/空**: 双向
- **离场条件**:
  - 多头: 价格回到中轨时平仓
  - 空头: 价格回到中轨时平仓
- **止损**: 是
- **默认值**:
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Mixed
  - 方向: 双向
  - 指标: MACD Bollinger
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
