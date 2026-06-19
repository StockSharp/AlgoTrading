# Scalping 15m EMA MACD RSI ATR
[English](README.md) | [Русский](README_ru.md)

该策略结合50周期EMA趋势过滤、MACD柱状图动量和RSI水平，使用ATR动态设定止损和止盈。

当价格位于EMA之上、MACD柱状图为正且RSI介于50与超买线之间时做多；当价格低于EMA、柱状图为负且RSI介于超卖线与50之间时做空。止损和目标随收盘价按ATR倍数移动。

## 细节

- **入场条件**：价格相对EMA的位置、MACD柱状图符号、RSI水平。
- **多空方向**：双向。
- **出场条件**：基于ATR的止损或止盈。
- **止损**：是。
- **默认值**：
  - `EmaPeriod` = 50
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `RsiPeriod` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `AtrPeriod` = 14
  - `SlAtrMultiplier` = 1m
  - `TpAtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(15)
- **筛选**：
  - 分类: Scalping
  - 方向: 双向
  - 指标: EMA, MACD, RSI, ATR
  - 止损: 是
  - 复杂度: 基础
  - 时间框架: 日内 (15m)
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
