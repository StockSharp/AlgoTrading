# Up3x1 Investor Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Up3x1 Investor strategy ports the classic MetaTrader expert advisor that reacts to strong expansion candles. It watches the latest completed bar on the configured timeframe and opens a new position on the following bar if the previous range and body were wide enough in the direction of the close.

The strategy is designed for discretionary markets such as forex majors on the H1 chart, but the thresholds can be tuned for other symbols. Only one position is kept at a time and every order uses the strategy `Volume` property as the trade size.

## Trading Logic

- **Signal Source** – completed time-frame candles from `CandleType` (default: 1 hour).
- **Entry Conditions**
  - Compute the high–low range and absolute candle body of the previous bar.
  - Enter long if the candle closed above the open and both the range and body exceed their respective pip thresholds.
  - Enter short if the candle closed below the open and both the range and body exceed the thresholds.
  - Ignore new entries while any position is open.
- **Position Management**
  - Optional stop-loss and take-profit levels are converted from pips to price units using `Security.PriceStep`.
  - A trailing stop activates once price advances by `TrailingStopPips + TrailingStepPips` from the entry.
  - The trailing stop only moves if the new level is at least `TrailingStepPips` closer to price than the previous trailing level.
  - The strategy exits a position when price touches the stop-loss, take-profit, or trailing stop levels.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `CandleType` | Data type of the candles used for signals (default: 1-hour time frame). |
| `RangeThresholdPips` | Minimum high–low distance of the previous candle, expressed in pips. |
| `BodyThresholdPips` | Minimum open–close distance of the previous candle, expressed in pips. |
| `StopLossPips` | Stop-loss distance in pips. Set to 0 to disable. |
| `TakeProfitPips` | Take-profit distance in pips. Set to 0 to disable. |
| `TrailingStopPips` | Distance maintained behind price when trailing. Set to 0 to disable trailing. |
| `TrailingStepPips` | Additional move in pips required before the trailing stop is tightened. |

> **Note:** Pip thresholds are multiplied by `Security.PriceStep`. Ensure the symbol has a valid `PriceStep` so that pip conversions reflect your instrument correctly.

## Usage Notes

1. Assign the target `Security` and trading connector before starting the strategy.
2. Adjust the pip thresholds to reflect the volatility of your market. Forex pairs with 5-digit quotes typically use 10 pips = 0.0010.
3. Set the strategy `Volume` to the desired order size. Position sizing logic from the original EA is intentionally simplified to keep the StockSharp version transparent.
4. Because signals are evaluated on closed candles, entries are sent immediately after confirmation of the expansion candle.
