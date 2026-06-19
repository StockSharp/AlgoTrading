# 多指标趋势跟随策略
[English](README.md) | [Русский](README_ru.md)

基于EMA交叉的策略，结合RSI和成交量确认，并使用ATR设置止损和止盈。

## 细节

- **入场条件**：快速EMA上穿/下穿慢速EMA，同时RSI确认且成交量高于均量
- **多空方向**：双向
- **出场条件**：基于ATR的止损和止盈
- **止损**：是，基于ATR
- **默认参数**：
  - `CandleType` = 5 分钟
  - `FastMaLength` = 10
  - `SlowMaLength` = 30
  - `RsiLength` = 14
  - `VolumeMaLength` = 20
  - `VolumeMultiplier` = 1.5
  - `AtrPeriod` = 14
  - `StopLossAtrMultiplier` = 2
  - `TakeProfitAtrMultiplier` = 3
- **筛选条件**：
  - 类别：趋势跟随
  - 方向：双向
  - 指标：EMA、RSI、ATR、成交量
  - 止损：是
  - 复杂度：中等
  - 时间框架：日内
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
