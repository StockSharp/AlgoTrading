# EMA WMA Crossover Strategy (中文)
[English](README.md) | [Русский](README_ru.md)

本策略基于指数移动平均线 (EMA) 与加权移动平均线 (WMA) 的交叉，计算使用的是K线开盘价。
当EMA从上向下穿越WMA时做多，当EMA从下向上穿越WMA时做空。
仓位大小按照账户权益风险百分比确定，并使用以tick为单位的固定止盈和止损。

## 详情

- **入场条件**:
  - 多头：`EMA crosses below WMA`
  - 空头：`EMA crosses above WMA`
- **多/空**：双向
- **出场条件**：止损或止盈
- **止损**：是
- **默认参数**：
  - `EmaPeriod` = 28
  - `WmaPeriod` = 8
  - `StopLossTicks` = 50
  - `TakeProfitTicks` = 50
  - `RiskPercent` = 10
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **过滤器**：
  - 分类：移动平均线交叉
  - 方向：双向
  - 指标：EMA, WMA
  - 止损：是
  - 复杂度：基础
  - 时间框架：日内
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中
