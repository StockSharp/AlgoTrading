# FRASMAv2
[English](README.md) | [Русский](README_ru.md)

基于 Fractal Adaptive Simple Moving Average (FRASMAv2) 的策略。

该策略使用 Fractal Dimension 指标计算分形自适应简单移动平均线。指标颜色根据斜率变化：上升为绿色，横盘为灰色，下跌为洋红色。策略监控最后一根已完成K线的颜色变化：

- 如果前一根K线颜色为绿色而最后一根变为非绿色（灰色或洋红色），策略将平掉空头并开立多头。
- 如果前一根K线颜色为洋红色而最后一根变为非洋红色，策略将平掉多头并开立空头。

风险控制通过以点数表示的止损和止盈参数完成。

## 细节

- **入场条件**：FRASMAv2 颜色变化。
- **多空方向**：双向。
- **出场条件**：相反的颜色变化。
- **止损**：通过保护模块设置止盈和止损。
- **默认值**：
  - `Period` = 30
  - `TakeProfit` = 2000 点
  - `StopLoss` = 1000 点
  - `CandleType` = TimeSpan.FromHours(4)
- **筛选条件**：
  - 分类：趋势反转
  - 方向：双向
  - 指标：FractalDimension, FRASMAv2
  - 止损：有
  - 复杂度：中等
  - 时间框架：4 小时
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
