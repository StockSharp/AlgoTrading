# Supertrend Fixed Tp Unified With Time Filter Msk 策略
[English](README.md) | [Русский](README_ru.md)

基于 Supertrend 指标的策略，采用固定百分比止盈，可选价格过滤和莫斯科时间过滤。

## 详情
- **入场条件**：Supertrend 方向变化并通过价格和时间过滤
- **多空方向**：可配置（多头、空头或同时）
- **离场条件**：达到止盈或出现反向信号
- **止损**：仅止盈
- **默认值**：
  - `AtrPeriod` = 23
  - `Factor` = 1.8m
  - `TakeProfitPercent` = 1.5m
  - `PriceFilter` = 10000m
  - `TimeFrom` = 0
  - `TimeTo` = 23
- **过滤器**：
  - 类型：趋势跟随
  - 方向：双向
  - 指标：Supertrend
  - 止损：是
  - 复杂度：基础
  - 时间框架：短期
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险级别：中等
