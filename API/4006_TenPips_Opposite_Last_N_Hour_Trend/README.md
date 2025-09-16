# Ten Pips Opposite Last N Hour Trend Strategy

## Overview

This strategy is a faithful port of the MetaTrader expert **10pipsOnceADayOppositeLastNHourTrend**. It trades exactly once per day at a configurable hour and deliberately takes the opposite side of the price change observed over the last *N* completed hourly candles. The logic is designed for currency pairs with five-digit pricing, but the C# version automatically adapts the pip size using the instrument's `PriceStep` and number of decimals.

At the selected trading hour the strategy inspects the closing price from `HoursToCheckTrend` hours ago and compares it with the close of the most recent completed hourly candle:

- If the older close is **higher**, the market has been falling (bearish), so the strategy opens a **long** position.
- Otherwise the market has been rising (bullish), therefore it opens a **short** position.

Positions are closed by protective stops, a daily time-based exit, or manually when the market is outside the trading window.

## Money management

Position sizing mirrors the original expert's martingale ladder:

1. The base volume comes from `FixedVolume`. When set to zero the strategy falls back to risk-based sizing using `Portfolio.CurrentValue * MaximumRisk / 1000` rounded to one decimal place.
2. The volume is limited by `MinimumVolume`, `MaximumVolume`, the instrument's volume limits, and a soft cap equal to `Portfolio.CurrentValue / 1000` lots.
3. After each closed trade the result is stored (up to the last five trades). When preparing a new entry the strategy scans that history and multiplies the lot size according to the first loss it finds, using the `FirstMultiplier` … `FifthMultiplier` sequence. This reproduces the nested `OrderSelect` checks from the MQL version.

## Risk controls

- `StopLossPips`, `TakeProfitPips`, and `TrailingStopPips` work in pip units. The port recalculates the pip size with the standard 3/5-decimal multiplier for Forex symbols.
- Trailing stops are symmetric for long and short positions. In the original MQL code the short-side trail never triggered because of a sign error; the C# version fixes that so both directions behave identically.
- `OrderMaxAge` closes any position that survives longer than the configured duration (21 hours by default).
- Outside of the allowed trading hour the strategy liquidates any open exposure to stay flat until the next session.
- `MaxOrders` guards against accidental re-entries by requiring that there are no open positions or active orders when a new signal is evaluated.

## Detailed workflow

1. Subscribe to hourly candles (the timeframe can be changed with `CandleType`).
2. Collect the close price of each finished candle in a small rolling buffer.
3. On the first completed candle at the allowed hour:
   - Check the portfolio/connection state and confirm no position is open.
   - Ensure we have at least `HoursToCheckTrend` historical candles to compare.
   - Determine the direction by comparing the current close with the close `HoursToCheckTrend` bars ago.
   - Compute the lot size using the money-management routine above and send a market order.
4. While a position is open the strategy:
   - Evaluates stop-loss, take-profit, and trailing levels using candle high/low prices.
   - Updates the trailing stop after new highs (for longs) or lows (for shorts).
   - Tracks the entry timestamp so it can enforce `OrderMaxAge`.
   - Records the realized profit/loss when the trade closes to feed the martingale multipliers.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `FixedVolume` | Fixed lot size. Set to `0` to use risk-based sizing. | `0.1` |
| `MinimumVolume` | Hard lower bound for the order volume. | `0.1` |
| `MaximumVolume` | Hard upper bound for the order volume. | `5` |
| `MaximumRisk` | Fraction of equity used when `FixedVolume = 0`. | `0.05` |
| `MaxOrders` | Maximum simultaneous orders/positions. | `1` |
| `TradingHour` | Hour of day (0–23) when new trades are allowed. | `7` |
| `HoursToCheckTrend` | Look-back window in hours for the trend comparison. | `30` |
| `OrderMaxAge` | Maximum lifetime of a position. | `21h` |
| `StopLossPips` | Stop-loss distance in pips. | `50` |
| `TakeProfitPips` | Take-profit distance in pips. | `10` |
| `TrailingStopPips` | Trailing-stop distance in pips. | `0` (disabled) |
| `FirstMultiplier` … `FifthMultiplier` | Lot multipliers applied when the most recent losing trade is found at the respective depth. | `4`, `2`, `5`, `5`, `1` |
| `CandleType` | Time frame for candle subscription. | `1 hour` |

## Differences from the original MQL expert

- Martingale sizing, order-aging, and trading window logic are reproduced one-to-one. The only deliberate change is the symmetrical short-side trailing stop to correct the sign bug in the original script.
- All protective levels are executed with market orders on the next finished candle because StockSharp strategies do not register separate stop/limit orders when using high-level helpers. This matches the behaviour of the original expert when its stop orders were triggered.
- Account equity is read from `Portfolio.CurrentValue`. If the adapter does not provide this field the strategy falls back to the base `Volume` (default `1`).
- The list of allowed trading hours mirrors the original array of `0…23`. To restrict trading to specific days you can edit `_tradingDayHours` inside the constructor.

## Usage notes

- Works best on hourly Forex data where pip size calculations using the `PriceStep` ×10 heuristic are valid.
- Always verify that `Security.VolumeStep`, `VolumeMin`, and `VolumeMax` are set by the connector so the strategy can adjust lot sizes correctly.
- Because entries are evaluated only once per finished candle, the strategy should be launched before the chosen trading hour so the first signal of the day is not missed.

