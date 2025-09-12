# SuperTrend Enhanced Pivot Reversal 策略
[English](README.md) | [Русский](README_ru.md)

该策略结合 SuperTrend 方向与枢轴高/低突破。在 SuperTrend 向下时，在最近的枢轴高上方挂多头止损单；在 SuperTrend 向上时，在枢轴低下方挂空头止损单。仓位以枢轴价格的百分比止损保护。

## 细节

- **入场条件**：
  - 多头：形成枢轴高，SuperTrend 向下 → 在枢轴上方挂多头止损。
  - 空头：形成枢轴低，SuperTrend 向上 → 在枢轴下方挂空头止损。
- **方向**：可配置。
- **出场条件**：百分比止损或在单向模式下方向翻转。
- **指标**：SuperTrend，枢轴高/低。
- **默认值**：
  - `LeftBars` = 6
  - `RightBars` = 3
  - `AtrLength` = 5
  - `Factor` = 2.618
  - `StopLossPercent` = 20
  - `TradeDirection` = Both
  - `CandleType` = 5 分钟
