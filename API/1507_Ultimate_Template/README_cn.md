# Ultimate Strategy Template
[English](README.md) | [Русский](README_ru.md)

基于快慢移动平均线交叉的模板策略，当快线与慢线交叉时开仓。包含可选的百分比止损和止盈保护。

## 详情

- **入场条件**：快线与慢线交叉。
- **多空方向**：双向。
- **出场条件**：反向交叉或风险控制触发。
- **止损**：百分比止损和止盈。
- **默认值**：
  - `FastLength` = 9
  - `SlowLength` = 21
  - `StopLossPercent` = 1
  - `TakeProfitPercent` = 3
- **筛选**：
  - 类型：趋势跟随
  - 方向：双向
  - 指标：SMA
  - 止损：支持
  - 复杂度：基础
  - 时间框架：中期
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险级别：中
