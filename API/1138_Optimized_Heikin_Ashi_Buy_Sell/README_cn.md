# 优化的 Heikin-Ashi 买卖可选策略
[English](README.md) | [Русский](README_ru.md)

Heikin-Ashi 蜡烛能够平滑价格并突出趋势方向。本策略一次只交易一个方向：在绿色蜡烛买入或在红色蜡烛卖出，并可限定交易日期范围。可选的止损和止盈参数用于风险控制。

## 细节

- **入场条件**：Heikin-Ashi 蜡烛颜色变化。
- **多空方向**：可配置。
- **离场条件**：反向信号或止损止盈。
- **止损止盈**：可选，百分比。
- **默认值**：
  - `CandleType` = 1 天
  - `StartDate` = 2023-01-01
  - `EndDate` = 2024-01-01
  - `TradeType` = BuyOnly
  - `UseStopLoss` = true
  - `StopLossPercent` = 2
  - `UseTakeProfit` = true
  - `TakeProfitPercent` = 4
- **筛选条件**：
  - 类别: 趋势
  - 方向: 可配置
  - 指标: Heikin-Ashi
  - 止损: 可选
  - 复杂度: 基础
  - 时间框架: 日线
  - 季节性: 日期范围
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中等

