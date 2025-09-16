# Gaps Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

A price action strategy that reacts to opening gaps between consecutive candles. It waits for a new bar to open beyond the prior
 high or low by a configurable pip distance, enters in the direction of the expected reversion, and manages the trade with fixed
 stops, targets, and an optional stepped trailing stop.

## How It Works

1. The strategy monitors a single symbol using the selected timeframe.
2. When a new candle is formed, it compares the opening price with the previous candle:
   - If the open is below the previous low minus `GapPips`, the strategy enters a long position expecting a bullish retracement.
   - If the open is above the previous high plus `GapPips`, it enters a short position anticipating a downward correction.
3. Once in a trade, risk management is handled entirely inside the strategy:
   - A fixed stop-loss is placed `StopLossPips` below (for long) or above (for short) the entry price.
   - A fixed take-profit is set `TakeProfitPips` away from the entry in the direction of the trade.
   - A trailing stop can be enabled; it only moves after price has advanced by `TrailingStopPips + TrailingStepPips` and then
     locks in profits by keeping the stop `TrailingStopPips` away from the most favorable price.
4. Protective levels are evaluated on every completed candle using high/low extremes so intrabar touches trigger exits reliably.
5. Open orders are cancelled before taking a new position, and position reversals automatically close the opposite side.

## Parameters

- `OrderVolume` = 0.1 — trading volume in lots for each new position.
- `StopLossPips` = 50 — distance from the entry price to the stop-loss level in pips. Set to 0 to disable the stop.
- `TakeProfitPips` = 50 — distance from the entry price to the take-profit level in pips. Set to 0 to disable the target.
- `TrailingStopPips` = 5 — size of the trailing stop in pips. Set to 0 to turn off trailing.
- `TrailingStepPips` = 5 — minimum price improvement (in pips) required before the trailing stop moves again.
- `GapPips` = 1 — minimal opening gap, expressed in pips, required to generate an entry signal.
- `CandleType` = 1-hour time frame — candles used for gap detection and position management.

## Implementation Notes

- Pip-based inputs are converted to absolute price distances using the instrument tick size. Five-digit and three-digit forex
  quotes are automatically adjusted to work with true pip values.
- Trailing stop logic requires `TrailingStepPips` to be positive when `TrailingStopPips` is enabled; otherwise the strategy throws
  an exception at startup, mirroring the original MQL validation.
- The strategy evaluates risk controls only on finished candles in accordance with the StockSharp high-level API guidelines.
- Manual stop and target management relies on market orders, so there are no separate protective orders resting in the book.
- Default settings assume forex instruments; adjust the pip distances when trading assets with different volatility or tick sizes.
