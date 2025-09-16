# Aeron JJN Scalper EA Strategy

## Overview
This strategy is a high-level StockSharp port of the **Aeron JJN Scalper** expert advisor. It watches finished candles, identifies specific two-bar reversal situations, and places simulated stop orders at the open of the most recent opposite candle. When the market reaches the stored stop level the strategy enters with a market order, applies ATR-based risk targets, and manages the trade with a pip-based trailing stop.

Key ideas:

* Trade direction is decided by a bullish/bearish two-candle reversal pattern.
* Entry levels come from the open price of the last strong candle in the opposite direction.
* An ATR(8) value measured on the signal bar sets both stop-loss and take-profit distances.
* Trailing stop logic moves the protective level once price advances by the configured pip offsets.
* Pending levels automatically expire after the configured number of minutes.

## Trading rules
### Signal detection
1. Work only with finished candles from the configured timeframe (default: 1 minute).
2. Compute pip size from the security price step and multiply by 10 for 3 or 5 decimal pricing to mimic MetaTrader pip behaviour.
3. Maintain a rolling window of the last 120 candles to search for reference bars.
4. Detect a **long setup** when:
   * The current candle closes above its open (bullish), and
   * The previous candle is bearish with body size greater than `DojiDiff1Pips`.
   * Search backwards for the latest bearish candle whose body exceeds `DojiDiff2Pips`; its open price becomes the buy stop level.
5. Detect a **short setup** when:
   * The current candle closes below its open (bearish), and
   * The previous candle is bullish with body size greater than `DojiDiff1Pips`.
   * Search backwards for the latest bullish candle whose body exceeds `DojiDiff2Pips`; its open price becomes the sell stop level.
6. Ignore new setups if there is already a pending level in the same direction, or if the ATR value for the candle is not yet available.

### Pending level management
* The stored level is treated as a pending stop order. It is discarded if price remains below (long) or above (short) the trigger until the expiration time `ResetMinutes` elapses.
* When price touches the level on a later candle (high ≥ buy level or low ≤ sell level), the strategy sends a market order sized to flip any existing exposure and add `Volume` contracts.
* Entering a long position clears any outstanding short level and vice versa.

### Stop-loss, take-profit, and trailing
* Upon entry the strategy records the ATR(8) value from the signal candle.
  * Long trades: stop-loss = `entry - ATR`, take-profit = `entry + ATR`.
  * Short trades: stop-loss = `entry + ATR`, take-profit = `entry - ATR`.
* On every finished candle the strategy:
  * Checks whether price reached the stop-loss or take-profit and exits with a market order if touched.
  * Applies trailing when price has moved at least `TrailingStopPips + TrailingStepPips` in favour of the position. The new stop sits `TrailingStopPips` behind the latest close. The stop never moves backwards.
* If the position is closed manually the internal state resets automatically.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `Volume` | 0.1 | Net position size used for entries; the strategy adds the absolute current position to flip direction when required. |
| `TrailingStopPips` | 5 | Base trailing stop distance (converted to price units). |
| `TrailingStepPips` | 5 | Extra advance required before moving the trailing stop again. |
| `ResetMinutes` | 10 | Expiration time for a stored pending level (minutes). |
| `DojiDiff1Pips` | 10 | Minimum body size (in pips) for the reversal candle that precedes the signal. |
| `DojiDiff2Pips` | 4 | Minimum body size (in pips) for the candle used as the entry reference level. |
| `CandleType` | 1 minute time frame | Candle data type used for calculations. |

## Implementation notes
* The strategy operates purely on finished candles and uses in-memory levels instead of real stop orders; when the level is breached a market order is sent immediately. This mirrors the original EA behaviour within the StockSharp high-level API.
* ATR(8) is computed with `AverageTrueRange` and cached so that the original stop/take distances remain constant for each trade.
* The pip conversion reproduces the MetaTrader adjustment for 3- and 5-digit quotes. If the security lacks `PriceStep`, a default step of `1` is used.
* Up to 120 historical candles are stored to replicate the original `CopyRates` look-back of 100 bars with some safety margin.
* No Python port is provided for this strategy.

## Usage
1. Attach the strategy to the desired security and portfolio.
2. Adjust the candle timeframe, pip offsets, and ATR-based filters to suit the instrument.
3. Start the strategy; it will track signals, submit market orders when trigger levels are touched, and manage exits automatically.
