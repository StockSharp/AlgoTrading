# Volatility Momentum Breakout 策略
[English](README.md) | [Русский](README_ru.md)

该策略结合基于ATR的突破水平、EMA趋势过滤以及RSI动量来捕捉强劲的价格运动。

## 细节

- **入场条件**: 价格收盘高于/低于ATR突破水平并得到EMA和RSI确认
- **多空方向**: 双向
- **出场条件**: 基于ATR的止损和1:2风险回报的止盈
- **止损**: ATR
- **默认值**:
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 1.5
  - `Lookback` = 20
  - `EmaPeriod` = 50
  - `RsiPeriod` = 14
  - `RsiLongThreshold` = 50
  - `RsiShortThreshold` = 50
  - `RiskReward` = 2
  - `AtrStopMultiplier` = 1
- **过滤器**:
  - 分类: 突破
  - 方向: 双向
  - 指标: ATR, EMA, RSI, Highest, Lowest
  - 止损: ATR
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
