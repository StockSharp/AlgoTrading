# MartinGale Scalping 策略
[English](README.md) | [Русский](README_ru.md)

当 `SMA(3)` 与 `SMA(8)` 交叉时进入仓位，并采用马丁格尔方式加仓，直到触发止损或止盈。

## 细节

- **入场条件**：`SMA3` 高于 `SMA8` 做多，低于则做空；信号持续时每根K线加仓。
- **多空**：通过 `TradingMode` 设置。
- **出场条件**：价格达到 `TakeProfit` 或 `StopLoss` 且 SMA 关系反转。
- **止损**：是，基于慢速 SMA。
- **默认值**：
  - `FastLength` = 3
  - `SlowLength` = 8
  - `TakeProfit` = 1.03
  - `StopLoss` = 0.95
  - `TradingMode` = Long
  - `CandleType` = 5 分钟
  - `MaxPyramids` = 5
- **过滤器**：
  - 类别: Trend
  - 方向: 可配置
  - 指标: SMA
  - 止损: 是
  - 复杂度: 初级
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 高
