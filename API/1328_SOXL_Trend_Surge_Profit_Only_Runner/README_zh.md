# SOXL Trend Surge Profit-Only Runner 策略
[English](README.md) | [Русский](README_ru.md)

该策略在价格位于200 EMA之上且 SuperTrend 为多头时做多。它要求 ATR 上升、成交量高于均值、时间过滤以及价格脱离 EMA 缓冲区。达到 ATR 目标后平仓一半仓位，其余部分使用 ATR 跟踪止损。

## 细节

- **入场条件**: 价格高于 EMA，SuperTrend 向上，成交量高于均值，ATR 上升，价格超出 EMA 缓冲区，时间在 14–19 点之间，出场后冷却
- **多空方向**: 仅做多
- **出场条件**: ATR 目标平仓 50%，剩余仓位使用跟踪止损
- **止损**: 跟踪止损
- **默认值**:
  - `EmaLength` = 200
  - `AtrLength` = 14
  - `AtrMultTarget` = 2.0
  - `CooldownBars` = 15
  - `SupertrendFactor` = 3.0
  - `SupertrendAtrPeriod` = 10
  - `MinBarsHeld` = 2
  - `VolFilterLen` = 20
  - `EmaBuffer` = 0.005
- **过滤器**:
  - 分类: 趋势
  - 方向: 做多
  - 指标: EMA、ATR、SuperTrend、成交量
  - 止损: 跟踪
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
