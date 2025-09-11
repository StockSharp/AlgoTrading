# Distance to Demand Vector 策略
[English](README.md) | [Русский](README_ru.md)

该策略基于 Distance to Demand Vector 指标。它比较多空需求向量的距离，并在其交叉时交易。

## 细节

- **入场条件**：
  - 多头：到多头向量的距离 > 到空头向量的距离
  - 空头：到多头向量的距离 < 到空头向量的距离
- **方向**：多空双向
- **出场条件**：
  - 相反信号
- **止损**：无
- **默认值**：
  - `Length` = 100
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **筛选**：
  - 类型：趋势
  - 方向：双向
  - 指标：Highest, Lowest
  - 止损：否
  - 复杂度：基础
  - 时间框架：短期
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中
