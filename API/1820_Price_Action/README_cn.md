# Price Action 策略
[English](README.md) | [Русский](README_ru.md)

**Price Action 策略** 在上一笔交易平仓后交替开立多头和空头市价单。
该策略使用固定止损距离、基于倍数的止盈目标，以及可选的带步长的跟踪止损。

## 细节
- **入场条件：** 没有持仓。方向在每次交易后在买入与卖出之间切换。
- **多头/空头：** 支持两种方向。
- **出场条件：** 价格触及跟踪止损、初始止损或止盈水平。
- **止损：** 固定距离止损，可选跟踪止损（步长定义更新所需的最小价格移动）。
- **默认值：** `Volume = 1`, `TP = 100`, `Leverage = 5`, `TrailingStop = 0`, `TrailingStep = 0`, `InitialDirection = Buy`, `CandleType = TimeSpan.FromMinutes(1).TimeFrame()`。
