# Linear On MACD
[English](README.md) | [Русский](README_ru.md)

该策略结合价格与成交量的 MACD 信号以及线性回归。

## 详情

- **入场条件**：当价格与成交量的 MACD 同时高于信号线且回归价格位于开盘与收盘之间时做多；相反条件做空。
- **多/空**：双向。
- **出场条件**：反向信号。
- **止损**：无。
- **默认值**：
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `Lookback` = 21
  - `RiskHigh` = false
  - `CandleType` = TimeSpan.FromMinutes(5)
- **筛选**：
  - 类型：趋势
  - 方向：双向
  - 指标：MACD, 线性回归
  - 止损：无
  - 复杂度：中等
  - 周期：日内
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险级别：可变
