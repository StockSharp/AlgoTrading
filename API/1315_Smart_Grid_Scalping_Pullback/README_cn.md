# Smart Grid Scalping Pullback Strategy
[English](README.md) | [Русский](README_ru.md)

基于ATR的网格剥头皮策略，从二十根K线前的价格扩展网格。进入前使用RSI过滤回调。仓位通过盈利目标或ATR跟踪止损退出。

## 细节

- **入场条件**:
  - 多头：close < basePrice - (LongLevel + 1) * ATR * GridFactor && range/low > NoTradeZone && RSI < MaxRsiLong && close > open
  - 空头：close > basePrice + (ShortLevel + 1) * ATR * GridFactor && range/high > NoTradeZone && RSI > MinRsiShort && close < open
- **多空方向**: 双向
- **出场条件**: 盈利目标或ATR跟踪止损
- **止损**: ATR跟踪止损
- **默认值**:
  - `AtrLength` = 10
  - `GridFactor` = 0.35m
  - `ProfitTarget` = 0.004m
  - `NoTradeZone` = 0.003m
  - `ShortLevel` = 5
  - `LongLevel` = 5
  - `MinRsiShort` = 70
  - `MaxRsiLong` = 30
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **过滤器**:
  - 类别: Scalping
  - 方向: 双向
  - 指标: ATR, RSI
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 短期
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
