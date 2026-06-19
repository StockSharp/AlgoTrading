# VWAP RSI Scalper FINAL v1
[English](README.md) | [Русский](README_ru.md)

基于VWAP和RSI的剥头皮策略，使用ATR止损和每日交易限制。

## 细节

- **入场条件**：价格相对VWAP与EMA并满足RSI阈值，且在交易时段内。
- **多空方向**：双向。
- **出场条件**：基于ATR的止损与止盈。
- **止损**：是。
- **默认值**：
  - `RsiLength` = 3
  - `RsiOversold` = 35m
  - `RsiOverbought` = 70m
  - `EmaLength` = 50
  - `SessionStart` = 09:00
  - `SessionEnd` = 16:00
  - `MaxTradesPerDay` = 3
  - `AtrLength` = 14
  - `StopAtrMult` = 1m
  - `TargetAtrMult` = 2m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **筛选**：
  - 分类：剥头皮
  - 方向：双向
  - 指标：VWAP, RSI, EMA, ATR
  - 止损：是
  - 复杂度：中等
  - 时间框架：日内(1m)
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
