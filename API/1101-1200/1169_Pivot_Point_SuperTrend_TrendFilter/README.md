# Pivot Point SuperTrend TrendFilter Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Combines a pivot-based SuperTrend line with a SuperTrend trend filter and a moving average confirmation. Trades when the trend flips or when a pivot SuperTrend signal appears within a date window.

## Details

- **Entry Criteria**:
  - Long: the price SuperTrend flips up and close is above the close-price SMA, or the pivot SuperTrend flips up within the date window.
  - Short: the price SuperTrend flips down and close is below the SMA, or the pivot SuperTrend flips down within the date window.
- **Exit Criteria**:
  - Close the entire long on either downward flip; close the entire short on either upward flip, regardless of SMA or dates.
  - Reverse only when the opposite entry is confirmed. Simultaneous opposing flips close an open position without a new entry.
- **Stops**: None
- **Default Values**:
  - `PivotPeriod` = 2
  - `Factor` = 3
  - `AtrPeriod` = 10
  - `TrendAtrPeriod` = 10
  - `TrendMultiplier` = 3
  - `MaPeriod` = 20
  - `CandleType` = 5 minutes
  - `StartDate` / `EndDate` = UTC `DateTime.MinValue` / `DateTime.MaxValue`
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Pivot, SuperTrend, SMA
  - Stops: No
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: Optional
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

## Calculation and execution

Only finished candles are used. A pivot high/low must be strictly above/below all `PivotPeriod` candles on each side; it becomes available only after the right-hand candles finish. If one candle qualifies as both, its high takes precedence. The first confirmed pivot seeds `center`; later pivots update it as `(2 * center + pivot) / 3`.

Pivot bands use Wilder ATR: the first `AtrPeriod` true ranges are averaged, then smoothed with weight `1 / AtrPeriod`. Basic support/resistance are `center ± Factor * ATR`. Support carries the greater of its basic and previous values when previous close is above previous support; otherwise it resets. Resistance carries the smaller value when previous close is below previous resistance; otherwise it resets. Close strictly above previous resistance switches up; strictly below previous support switches down; otherwise direction persists. Initial direction is up and initialization is not a signal.

The separate price filter uses StockSharp's native SuperTrend (`TrendAtrPeriod`, `TrendMultiplier`). SMA includes the current close. Signals are changes of direction, not persistent entry conditions; both ATRs, SMA and a pivot center must be ready. `StartDate <= candle.OpenTime <= EndDate` gates only pivot entries. Dates must be ordered UTC values. Each confirmed entry targets `Volume`; a reversal orders `Volume + abs(Position)`, a close orders `abs(Position)`. There is no cooldown or protective stop.

Pivot-based concept: [LonesomeTheBlue's Pivot Point Supertrend](https://www.tradingview.com/script/L0AIiLvH-Pivot-Point-Supertrend/). The rules above define this example's two-direction model.
