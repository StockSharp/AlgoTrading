# Fearzone Panel
[English](README.md) | [Русский](README_ru.md)

基于《Framgångsrik Aktiehandel》中的 FearZone 面板的策略，用于寻找由恐慌驱动的极端下跌。

策略等待两个 Fearzone 指标同时激活，并且至少一个恐慌触发条件满足，同时价格保持在200周期均线之上。

## 详情

- **入场条件**：FZ1 和 FZ2 激活，且负向冲击、反弹区或随机指标超卖满足之一，并且收盘价高于 MA200。
- **多空方向**：仅做多。
- **出场条件**：价格跌破 MA200。
- **止损**：无。
- **默认值**：
  - `LookbackPeriod` = 22
  - `BollingerPeriod` = 200
  - `ImpulsePeriod` = 10
  - `ImpulsePercent` = 0.1m
  - `MaPeriod` = 200
  - `StochThreshold` = 30m
  - `CandleType` = TimeSpan.FromDays(1)
- **过滤条件**：
  - 类别：均值回归
  - 方向：多头
  - 指标：BollingerBands, RateOfChange, StochasticOscillator, SimpleMovingAverage, Highest
  - 止损：无
  - 复杂度：中等
  - 时间框架：日线
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
