# Heiken Ashi Supertrend ATR-SL 策略
[English](README.md) | [Русский](README_ru.md)

该策略结合 Heikin Ashi 蜡烛与 Supertrend 趋势过滤。进场要求无影线的蜡烛，并可启用基于 ATR 的止损与保本。

## 细节

- **进场条件**：
  - 多头：绿色 HA 蜡烛且无下影线，可选上升趋势过滤
  - 空头：红色 HA 蜡烛且无上影线，可选下降趋势过滤
- **多/空**：均支持
- **出场条件**：
  - 多头：红色 HA 蜡烛且无上影线或触发止损
  - 空头：绿色 HA 蜡烛且无下影线或触发止损
- **止损**：基于 ATR，可选保本
- **默认值**：
  - `UseSupertrend` = true
  - `AtrPeriod` = 10
  - `AtrFactor` = 3m
  - `UseBreakEven` = false
  - `BreakEvenAtrMultiplier` = 1m
  - `UseHardStop` = false
  - `StopLossAtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **筛选**：
  - 类别：趋势跟随
  - 方向：双向
  - 指标：Heikin Ashi, Supertrend, ATR
  - 止损：有
  - 复杂度：中等
  - 时间框架：中期
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
