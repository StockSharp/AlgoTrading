# 多时间框架 Parabolic SAR
[English](README.md) | [Русский](README_ru.md)

该策略结合多个时间框架上的 Parabolic SAR 信号。当价格高于所选 SAR 时做多，价格跌破 SAR 时做空。支持可选的止损、追踪止损和止盈。

## 详情

- **入场条件**：
  - **多头**：价格高于 `LongSource` 指定的 SAR。
  - **空头**：价格低于 `ShortSource` 指定的 SAR。
- **出场条件**：
  - 价格与 SAR 反向穿越或触发保护。
- **指标**：
  - 当前时间框架的 Parabolic SAR
  - 可选的高阶与低阶时间框架 Parabolic SAR
- **止损**：通过 StartProtection 设置的止损、追踪止损和止盈（可选）。
- **默认值**：
  - `Acceleration` = 0.02
  - `MaxAcceleration` = 0.2
  - `StopLossPercent` = 1
  - `TrailingPercent` = 0.5
  - `TakeProfitPercent` = 2
- **过滤条件**：
  - 时间框架：主 5m，高阶 1d，低阶 1m
  - 指标：Parabolic SAR
  - 止损：可选
  - 复杂度：中等
