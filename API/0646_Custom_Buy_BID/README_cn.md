# Custom Buy BID 策略
[English](README.md) | [Русский](README_ru.md)

该策略使用 Supertrend 指标来识别向上的趋势反转。当价格上穿 Supertrend 线时开多单，并通过可调的止盈和止损百分比来控制风险。

## 细节

- **入场条件**：价格上穿 Supertrend。
- **方向**：仅多头。
- **出场条件**：止盈或止损。
- **止损**：有。
- **默认值**：
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3m
  - `TakeProfitPercent` = 5m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StartDate` = 2018-09-01
  - `EndDate` = 9999-01-01
- **筛选**：
  - 类型：趋势跟随
  - 方向：多头
  - 指标：Supertrend
  - 止损：有
  - 复杂度：基础
  - 时间框架：短期
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中
