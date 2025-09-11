# 双Keltner通道
[English](README.md) | [Русский](README_ru.md)

**双Keltner通道**策略使用两个具有不同倍数的Keltner通道来捕捉突破。
当价格穿越外通道并返回穿过内通道时开仓。
止损和止盈采用固定百分比管理。

## 详情
- **入场条件**：价格穿越外通道并沿同一方向重新穿越内通道。
- **多头/空头**：双向。
- **出场条件**：止损、止盈或反向信号。
- **止损**：是，基于百分比。
- **默认值**：
  - `EmaPeriod = 50`
  - `InnerMultiplier = 2.75m`
  - `OuterMultiplier = 3.75m`
  - `MaxStopPercent = 10m`
  - `SlTpRatio = 1m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **过滤器**：
  - 类别: 突破
  - 方向: 双向
  - 指标: Keltner
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内 (5m)
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
