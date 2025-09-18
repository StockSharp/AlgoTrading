# Explosion Strategy

The strategy reproduces the logic of the MetaTrader "Explosion" expert. It watches the range of each finished candle and enters the market when the latest bar "explodes" – its height more than doubles the range of the previous bar. The direction is decided by the candle body: a bullish body opens a long position, while a bearish body opens a short one.

## Trading rules

1. Process only finished candles coming from the configured `CandleType` subscription.
2. Compute the current range as `High - Low` and compare it with the range of the previous candle.
3. A **long** entry is opened when `currentRange > previousRange * 2` and the close is above the open.
4. A **short** entry is opened when `currentRange > previousRange * 2` and the close is below the open.
5. When `OnlyOnePositionPerBar` is enabled, at most one trade per direction is allowed for a candle open time. Attempts on the same bar are ignored.
6. The strategy keeps a single net position, therefore an opposite trade automatically closes any existing exposure before establishing the new direction.
7. Protective mechanics:
   - `StopLossPips` and `TakeProfitPips` place virtual exit levels measured in pips from the entry price.
   - `TrailingStopPips` and `TrailingStepPips` move the stop once price travels in favour of the position by at least the trailing distance and every additional step.
   - The optional pip multiplier emulates the MQL auto-digits helper by multiplying the pip size by 10 on 3- and 5-digit instruments.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `TradeVolume` | `0.01` | Market order volume used on entries. |
| `StopLossPips` | `20` | Stop-loss distance in pips. Zero disables the stop. |
| `TakeProfitPips` | `10` | Take-profit distance in pips. Zero disables the take. |
| `TrailingStopPips` | `25` | Activation distance for the trailing stop in pips. Zero disables trailing. |
| `TrailingStepPips` | `5` | Additional move in pips required before the trailing stop advances. Must stay positive when trailing is enabled. |
| `UseAutoPipMultiplier` | `true` | Multiply the pip size by 10 on instruments with 3 or 5 decimal places, matching the MQL auto-digits helper. |
| `OnlyOnePositionPerBar` | `true` | Forbid more than one entry per bar open time. |
| `CandleType` | `TimeSpan.FromMinutes(1).TimeFrame()` | Candle series used for calculations. |

## Notes on the conversion

- StockSharp works with a net position, so hedging multiple simultaneous orders of the original Expert Advisor is not supported. The behaviour is equivalent to enabling `OnlyOneOpenedPos` in the MQL version.
- Trailing stop updates are performed on candle closes instead of every tick. The logic matches the original thresholds while remaining compatible with the high-level API.
- The pip multiplier reproduces the automatic digit detection that scales distances by 10 on 5-digit forex symbols.

## Suggested usage

1. Choose the instrument and timeframe that match the original expert (e.g., the recommended M15/M30 charts for forex pairs).
2. Adjust the pip-based risk parameters to the instrument's volatility.
3. Enable logging to monitor when the trailing stop advances and how protective levels are recalculated.
