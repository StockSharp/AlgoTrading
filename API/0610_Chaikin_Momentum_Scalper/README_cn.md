# Chaikin Momentum Scalper
[English](README.md) | [Русский](README_ru.md)

该剥头皮策略利用Chaikin振荡器捕捉动量变化。当振荡器上穿零且价格位于200周期SMA之上时做多；当振荡器下穿零且价格在SMA之下时做空。ATR倍数用于设置止损和止盈。

## 详情

- **入场条件**：Chaikin振荡器上/下穿零且价格在SMA之上/之下。
- **多空方向**：双向。
- **出场条件**：基于ATR的止损与止盈。
- **止损**：是。
- **默认值**：
  - `FastLength` = 3
  - `SlowLength` = 10
  - `SmaLength` = 200
  - `AtrLength` = 14
  - `AtrMultiplierSL` = 1.5m
  - `AtrMultiplierTP` = 2.0m
  - `CandleType` = TimeSpan.FromHours(1)
- **过滤器**：
  - 分类：动量
  - 方向：双向
  - 指标：Chaikin Oscillator，SMA，ATR
  - 止损：是
  - 复杂度：基础
  - 时间框架：日内
  - 季节性：无
  - 神经网络：否
  - 背离：否
  - 风险级别：中等
