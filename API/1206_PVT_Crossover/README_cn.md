# PVT交叉策略
[English](README.md) | [Русский](README_ru.md)

该策略基于价格成交量趋势（PVT）与其指数移动平均线（EMA）的交叉进行交易。当PVT上穿其EMA时开多头，当下穿时开空头。

## 细节

- **入场条件**：
  - **多头**：PVT上穿其EMA。
  - **空头**：PVT下穿其EMA。
- **多空方向**：双向。
- **出场条件**：
  - 相反信号时反向开仓。
- **止损**：无。
- **默认值**：
  - `EmaLength` = 20。
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()。
- **过滤器**：
  - 分类：趋势
  - 方向：双向
  - 指标：PVT, EMA
  - 止损：无
  - 复杂度：基础
  - 时间框架：日内
