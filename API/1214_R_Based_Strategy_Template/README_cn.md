# R Based Strategy Template
[English](README.md) | [Русский](README_ru.md)

基于RSI的策略，具有风险控制仓位大小和多种止损类型。

## 细节

- **入场条件**：
  - 当RSI下穿`OversoldLevel`时做多。
  - 当RSI上穿`OverboughtLevel`时做空。
- **多空方向**：双向。
- **出场条件**：根据`TpRValue`倍数的止损或止盈。
- **止损**：
  - Fixed、Atr、Percentage 或 Ticks。
- **默认值**：
  - `RiskPerTradePercent` = 1
  - `RsiLength` = 14
  - `OversoldLevel` = 30
  - `OverboughtLevel` = 70
  - `StopLossType` = Fixed
  - `SlValue` = 100
  - `AtrLength` = 14
  - `AtrMultiplier` = 2
  - `TpRValue` = 2
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**：
  - 类别：振荡指标
  - 方向：双向
  - 指标：RSI、ATR
  - 止损：是
  - 复杂度：中等
  - 时间框架：任意
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
