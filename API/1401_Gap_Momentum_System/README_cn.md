# Gap Momentum System 策略
[English](README.md) | [Русский](README_ru.md)

实现 Perry Kaufman 的 Gap Momentum 系统。该策略比较向上与向下的累积缺口，当信号上升或下降时进行交易。

## 详情
- **入场条件**：信号上升 -> 买入，信号下降 -> 卖出或反向。
- **多空方向**：可配置。
- **出场条件**：相反信号。
- **止损**：无。
- **默认参数**：
  - `Period` = 40
  - `SignalPeriod` = 20
  - `LongOnly` = true
- **过滤器**：
  - 类型：动量
  - 方向：双向或仅做多
  - 指标：Gap momentum
  - 止损：无
  - 复杂度：低
  - 时间框架：Daily
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
