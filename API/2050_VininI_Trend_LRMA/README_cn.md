# VininI Trend LRMA 策略
[English](README.md) | [Русский](README_ru.md)

VininI Trend LRMA 策略使用线性回归移动平均线（LRMA）来追踪市场方向。策略支持两种入场模式：
- **Breakdown**：当 LRMA 突破预设的上/下水平时交易。
- **Twist**：当 LRMA 改变方向时交易。

## 细节

- **入场条件**：LRMA 突破水平（Breakdown）或方向反转（Twist）
- **多头/空头**：同时支持
- **出场条件**：反向信号
- **止损**：无
- **默认值**：
  - `CandleType` = TimeFrameCandle 4h
  - `Period` = 13
  - `UpLevel` = 10
  - `DnLevel` = -10
  - `Mode` = Breakdown
- **筛选器**：
  - 类别: Trend
  - 方向: Both
  - 指标: LinearRegression
  - 止损: None
  - 复杂度: Basic
  - 时间框架: Any
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险等级: Medium
