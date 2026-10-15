# Pivot Point Supertrend
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategy combining pivot points with an ATR-based Supertrend to capture trend reversals.

Pivot points define a dynamic center line. An ATR multiplier builds upper and lower bands that trail price. When the trend switches direction the strategy enters accordingly.

## Details

- **Entry Criteria**: Signals based on pivot points and ATR Supertrend.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal.
- **Stops**: No.
- **Default Values**:
  - `PivotPeriod` = 2
  - `AtrFactor` = 3m
  - `AtrPeriod` = 10
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Pivot Points, ATR
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

## Calculation and execution

Only finished candles are used. A pivot high/low must be strictly above/below all `PivotPeriod` candles on each side; it becomes available only after the right-hand candles finish. If one candle qualifies as both, its high takes precedence. The first confirmed pivot seeds `center`; later pivots update it as `(2 * center + pivot) / 3`.

Pivot bands use Wilder ATR: the first `AtrPeriod` true ranges are averaged, then smoothed with weight `1 / AtrPeriod`. Basic support/resistance are `center ± AtrFactor * ATR`. Support carries the greater of its basic and previous values when previous close is above previous support; otherwise it resets. Resistance carries the smaller value when previous close is below previous resistance; otherwise it resets. Close strictly above previous resistance switches up; strictly below previous support switches down; otherwise direction persists. Initial direction is up and initialization is not a signal.

Both the Wilder ATR and a confirmed pivot center must be ready. Only a change of the pivot-band direction enters: up opens long, down opens short. Initial direction is up; initialization produces no order. Every opposite signal fully reverses the position with one market order of `Volume + abs(Position)`; a flat entry orders `Volume`. There is no SMA, separate price SuperTrend, date filter, cooldown or protective stop.

Pivot-based concept: [LonesomeTheBlue's Pivot Point Supertrend](https://www.tradingview.com/script/L0AIiLvH-Pivot-Point-Supertrend/). The rules above define this example's two-direction model.
