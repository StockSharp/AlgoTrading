# 比特币动量
[English](README.md) | [Русский](README_ru.md)

仅在价格位于更高周期EMA之上且不存在警戒条件时交易比特币，并通过ATR拖尾止损保护利润。

## 详情

- **入场条件**: 价格高于周线EMA且无警戒状态。
- **多空方向**: 仅做多。
- **退出条件**: 价格跌破拖尾止损或周线EMA。
- **止损**: 基于ATR的拖尾止损。
- **默认值**:
  - `CandleType` = TimeSpan.FromDays(1)
  - `HigherCandleType` = TimeSpan.FromDays(7)
  - `EmaLength` = 20
  - `AtrLength` = 5
  - `TrailStopLookback` = 7
  - `TrailStopMultiplier` = 0.2m
  - `StartTime` = 2000-01-01
  - `EndTime` = 2099-01-01
- **过滤器**:
  - 类型: 动量
  - 方向: 多头
  - 指标: EMA, ATR, Highest
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日线
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中
