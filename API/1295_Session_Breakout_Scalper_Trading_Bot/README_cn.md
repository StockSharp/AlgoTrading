# Session Breakout Scalper Trading Bot 策略
[English](README.md) | [Русский](README_ru.md)

Session Breakout Scalper 在预定义的交易时段形成的价格区间突破时入场。

## 细节

- **入场条件**: 价格突破该时段的最高价或最低价
- **多空方向**: 双向
- **出场条件**: 止盈或止损
- **止损**: ATR 或固定
- **默认值**:
  - `SessionStart` = 01:00
  - `SessionEnd` = 02:00
  - `TakeProfit` = 100
  - `StopLoss` = 50
  - `UseAtrStop` = true
  - `AtrLength` = 14
  - `AtrMultiplier` = 2
  - `CandleType` = 1 分钟
- **过滤器**:
  - 分类: 突破
  - 方向: 双向
  - 指标: ATR
  - 止损: ATR
  - 复杂度: 基础
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
