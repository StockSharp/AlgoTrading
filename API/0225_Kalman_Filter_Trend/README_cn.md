# Kalman Filter Trend Strategy
[English](README.md) | [Русский](README_ru.md)

此趋势跟随方法使用卡尔曼滤波器平滑价格波动，并估计潜在方向。滤波器可动态适应市场噪声，比常规均线更能反映趋势力度。

当收盘价上穿卡尔曼估算值时做多；收盘价下穿时做空。由于滤波器每根K线更新，价格穿越即转仓，可持续参与趋势行情。ATR止损防止趋势突然反转带来的损失。

## 细节
- **入场条件**:
  - 多头: `Close > Kalman Filter`
  - 空头: `Close < Kalman Filter`
- **多/空**: 双向
- **离场条件**:
  - 多头: 收盘价跌破卡尔曼滤波
  - 空头: 收盘价升破卡尔曼滤波
- **止损**: 基于ATR
- **默认值**:
  - `ProcessNoise` = 0.01m
  - `MeasurementNoise` = 0.1m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Trend
  - 方向: 双向
  - 指标: Kalman Filter
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
