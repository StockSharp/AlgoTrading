# VWAP Pro V21
[English](README.md) | [Русский](README_ru.md)

策略结合快慢EMA、VWAP和基于ATR的风险控制。使用1小时50周期EMA作为趋势过滤器。当价格符合趋势时开仓，达到ATR计算的止盈或止损水平时平仓。

## 详情

- **入场条件**：价格高/低于快EMA、VWAP和趋势过滤器。
- **多空方向**：双向。
- **出场条件**：ATR止盈或止损。
- **止损**：有。
- **默认值**：
  - `EmaFastPeriod` = 9
  - `EmaSlowPeriod` = 21
  - `AtrPeriod` = 14
  - `TakeProfitAtrMultiplier` = 0.7
  - `StopLossAtrMultiplier` = 1.4
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**：
  - 类别：趋势跟随
  - 方向：双向
  - 指标：EMA, VWAP, ATR
  - 止损：有
  - 复杂度：基础
  - 时间框架：日内 (1m)
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
