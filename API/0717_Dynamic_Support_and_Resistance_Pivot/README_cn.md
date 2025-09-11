# 动态支撑阻力枢轴策略
[English](README.md) | [Русский](README_ru.md)

该策略通过最近的枢轴高点和低点构建动态支撑和阻力位。当价格在接近支撑位时向上穿越支撑买入，当价格在接近阻力位时向下穿越阻力卖出。风险管理使用固定百分比的止损和止盈。

## 细节

- **入场条件**：价格在 `SupportResistanceDistance` 百分比范围内接近支撑/阻力并发生穿越。
- **多头/空头**：同时。
- **出场条件**：固定止盈与止损。
- **止损**：是。
- **默认值**：
  - `PivotLength` = 2
  - `SupportResistanceDistance` = 0.4m
  - `StopLossPercent` = 10.0m
  - `TakeProfitPercent` = 26.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**：
  - 类别: 突破
  - 方向: 双向
  - 指标: 枢轴
  - 止损: 是
  - 复杂度: 初级
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
