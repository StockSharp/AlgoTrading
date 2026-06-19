# Nifty Options Trendy Markets with TSL Strategy
[English](README.md) | [Русский](README_ru.md)

基于布林带突破并结合ADX与Supertrend过滤的策略。入场需要成交量激增。持仓在MACD交叉、ADX减弱或基于ATR的追踪止损触发时平仓。

## 详情

- **入场条件**:
  - 多头：价格上穿布林带上轨 && ADX > 阈值 && 成交量激增 && 价格高于Supertrend
  - 空头：价格下穿布林带下轨 && ADX > 阈值 && 成交量激增 && 价格低于Supertrend
- **多空方向**: 双向
- **出场条件**: MACD交叉、ADX下降或ATR追踪止损
- **止损**: ATR追踪止损
- **默认值**:
  - `BollingerPeriod` = 20
  - `BollingerMultiplier` = 2m
  - `AdxLength` = 14
  - `AdxEntryThreshold` = 25m
  - `AdxExitThreshold` = 20m
  - `SuperTrendLength` = 10
  - `SuperTrendMultiplier` = 3m
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `AtrLength` = 14
  - `AtrMultiplier` = 1.5m
  - `VolumeSpikeMultiplier` = 1.5m
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **过滤器**:
  - 类别: Trend
  - 方向: 双向
  - 指标: Bollinger Bands, ADX, Supertrend, MACD, ATR
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 中期
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
