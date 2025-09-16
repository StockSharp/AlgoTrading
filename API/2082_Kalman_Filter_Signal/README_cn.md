# Kalman Filter Signal 策略
[English](README.md) | [Русский](README_ru.md)

该策略使用卡尔曼滤波器来识别方向变化。根据选择的信号模式，信号由价格与滤波值的关系或滤波器斜率决定。当信号变为看涨时开多单，信号变为看跌时开空单。出现反向信号时反转仓位。止损和止盈使用绝对数值。

## 详情

- **入场条件**:
  - 多单：信号转为看涨
  - 空单：信号转为看跌
- **多空方向**: 都可以
- **出场条件**: 反向信号
- **止损**: 绝对止损和止盈
- **默认值**:
  - `ProcessNoise` = 1.0
  - `MeasurementNoise` = 1.0
  - `CandleType` = TimeSpan.FromHours(3).TimeFrame()
  - `Mode` = SignalMode.Kalman
  - `StopLoss` = 1000m
  - `TakeProfit` = 2000m
- **过滤器**:
  - 类别: Trend following
  - 方向: 双向
  - 指标: Kalman Filter
  - 止损: 是
  - 复杂度: 中等
  - 周期: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
