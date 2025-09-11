# Eliora Gold 1m Heikin Ashi 策略
[Русский](README_ru.md) | [English](README.md)

该策略在一分钟图上使用 Heikin Ashi 蜡烛。强劲的蜡烛与趋势一致且无盘整时开仓，成交后等待数根 K 线的冷却期。离场基于 ATR 的移动止损。

## 细节

- **入场条件**：强烈的 Heikin Ashi 蜡烛、与 SMA 趋势一致、无盘整并通过波动性过滤。
- **多空**：双向。
- **离场条件**：ATR 移动止损。
- **止损**：有。
- **默认值**：
  - `AtrPeriod` = 14
  - `CooldownBars` = 5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **筛选器**：
  - 类别：趋势跟随
  - 方向：双向
  - 指标：Heikin Ashi, ATR, SMA, Highest/Lowest
  - 止损：有
  - 复杂度：中等
  - 时间框架：日内 (1m)
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
