# MADH Moving Average Difference, Hann 策略
[English](README.md) | [Русский](README_ru.md)

实现 John Ehlers 提出的 MADH 指标。当指标高于零时做多，低于零时做空。

## 详情
- **入场条件**：MADH > 0 做多，MADH < 0 做空。
- **多空方向**：双向。
- **出场条件**：反向信号时反手。
- **止损**：无。
- **默认参数**：
  - `ShortLength` = 8
  - `DominantCycle` = 27
- **过滤器**：
  - 类型：趋势跟随
  - 方向：双向
  - 指标：MADH
  - 止损：无
  - 复杂度：低
  - 时间框架：任意
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
