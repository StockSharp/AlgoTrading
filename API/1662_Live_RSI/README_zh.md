# Live RSI 策略
[English](README.md) | [Русский](README_ru.md)

使用多个 RSI 计算（close、weighted、typical、median、open）结合 Parabolic SAR 来检测趋势反转。当 RSI 数值按多头顺序排列且价格在 SAR 之上时做多；当 RSI 数值按空头顺序排列且价格在 SAR 之下时做空。SAR 值同时作为跟踪止损。

## 细节

- **入场条件**：
  - 多头：RSI 为多头顺序且价格高于 SAR。
  - 空头：RSI 为空头顺序且价格低于 SAR。
- **多空方向**：双向。
- **出场条件**：
  - 反向趋势信号或 SAR 跟踪止损。
- **止损**：可选的固定止损和基于 SAR 的跟踪止损。
- **默认值**：
  - `RSI Period` = 30
  - `SAR Step` = 0.08
  - `Stop Loss` = 40
  - `Check Hour` = false
  - `Start Hour` = 17
  - `End Hour` = 1
  - `Candle Type` = 1 hour
- **筛选**：
  - 类别: Trend Following
  - 方向: Long & Short
  - 指标: RSI, Parabolic SAR
  - 止损: 有
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 可选（时间过滤）
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中等
