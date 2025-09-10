# BBTrend SuperTrend Decision 策略
[English](README.md) | [Русский](README_ru.md)

该策略通过两个不同周期的布林带计算 **BBTrend** 值，并将其输入 SuperTrend 指标。
SuperTrend 的方向用于决定做多或做空。可以选择开启按百分比计算的止盈和止损保护。

## 详情

- **入场条件**:
  - 多头：SuperTrend 方向向上。
  - 空头：SuperTrend 方向向下。
- **多空方向**：可同时或分别启用。
- **出场条件**：
  - SuperTrend 方向反转。
- **止损止盈**：可选的百分比 TP/SL。
- **默认值**：
  - 短期 BB 长度 = 20，长期 BB 长度 = 50，StdDev = 2。
  - SuperTrend 长度 = 10，系数 = 7。
  - 止盈 = 30%，止损 = 20%。
- **筛选**：
  - 分类：趋势跟随
  - 方向：双向
  - 指标：Bollinger Bands, SuperTrend
  - 止损：可选 TP/SL
  - 复杂度：中等
  - 时间框架：短期
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
