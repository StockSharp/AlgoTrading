# Realtime Delta Volume Action 策略
[English](README.md) | [Русский](README_ru.md)

该策略监测每根K线中买卖量的差值。当量差超过阈值时开仓。

## 详情

- **入场条件**：量差高于/低于阈值。
- **多/空**：双向。
- **出场条件**：反向信号或止损。
- **止损**：是。
- **默认值**：
  - `DeltaThreshold` = 100
  - `CandleType` = TimeSpan.FromMinutes(1)
- **筛选器**：
  - 类别：突破
  - 方向：双向
  - 指标：量差
  - 止损：是
  - 复杂度：基础
  - 时间框架：日内
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
