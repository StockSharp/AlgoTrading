# Penrose Diagram 策略
[English](README.md) | [Русский](README_ru.md)

该策略基于开盘价到极值的距离绘制简化的 Penrose 三角形。图形使用最近周期的平均和最大突破距离。

## 详情

- **入场条件**：无。
- **多空方向**：无。
- **出场条件**：无。
- **止损**：无。
- **默认值**：
  - `Period` = 48
  - `CandleType` = TimeSpan.FromDays(1)
  - `Extend` = true
- **过滤器**：
  - 分类：Drawing
  - 方向：无
  - 指标：SMA、Highest
  - 止损：无
  - 复杂度：基础
  - 时间框架：日线
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：无
