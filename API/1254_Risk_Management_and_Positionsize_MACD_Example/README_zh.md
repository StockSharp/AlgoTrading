# Risk Management and Positionsize - MACD example
[English](README.md) | [Русский](README_ru.md)

策略 **Risk Management and Positionsize - MACD example** 展示了基于当前权益的动态仓位大小。它结合高时间框架的 MACD 金叉/死叉以及移动平均趋势过滤。

## 详情
- **入场条件**：MACD 线与信号线交叉，并得到趋势确认。
- **多空方向**：双向。
- **出场条件**：MACD 反向交叉。
- **止损**：无。
- **默认值**:
  - `InitialBalance = 10000m`
  - `LeverageEquity = true`
  - `MarginFactor = -0.5m`
  - `Quantity = 3.5m`
  - `MacdMaType = MovingAverageTypeEnum.EMA`
  - `FastMaLength = 11`
  - `SlowMaLength = 26`
  - `SignalMaLength = 9`
  - `MacdTimeFrame = TimeSpan.FromMinutes(30)`
  - `TrendMaType = MovingAverageTypeEnum.EMA`
  - `TrendMaLength = 55`
  - `TrendTimeFrame = TimeSpan.FromDays(1)`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **过滤器**:
  - 类别: 趋势跟随
  - 方向: 双向
  - 指标: MACD, Moving Average
  - 止损: 无
  - 复杂度: 中等
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中等
