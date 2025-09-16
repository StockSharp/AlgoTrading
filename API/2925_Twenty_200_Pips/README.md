# Twenty 200 Pips Strategy

## Overview
The strategy replicates the original **20/200 pips** MQL5 expert. It examines hourly candles and
compares two historical open prices (`Open[t1]` and `Open[t2]`). When the difference between these
opens exceeds a configurable delta during a specific hour, the strategy enters a single trade for the
session and relies on fixed take-profit and stop-loss levels.

## Trading logic
1. Subscribe to hourly candles (configurable) and feed the open price into two `Shift` indicators to
   retrieve the opens at the required indexes.
2. During every finished candle, reset the "can trade" flag once the current hour is greater than the
   configured trading hour. This mirrors the daily reset in the original expert adviser.
3. When the hour matches the configured trading hour and no position is open, compare the stored
   open prices:
   - If `Open[t1] > Open[t2] + delta`, submit a market **sell** order.
   - If `Open[t1] + delta < Open[t2]`, submit a market **buy** order.
4. After sending an order the strategy forbids new entries until the next daily reset. Protective
   take-profit and stop-loss orders are managed via `StartProtection`.

## Parameters
- `TakeProfit` – distance in price points for the take-profit order (default 200 points).
- `StopLoss` – distance in price points for the stop-loss order (default 2000 points).
- `TradeHour` – hour of the day when the entry check is performed (default 18).
- `FirstOffset` – index of the older open price (maps to `Open[t1]` in the MQL script, default 7).
- `SecondOffset` – index of the more recent open price (`Open[t2]`, default 2).
- `DeltaPoints` – minimum difference in points between the two opens to trigger a trade (default 70).
- `Volume` – order size used for market entries (default 0.1).
- `CandleType` – timeframe used for calculations (default 1-hour candles).

## Implementation notes
- `Shift` indicators are processed manually to access historical open prices without maintaining
  custom collections.
- The strategy calls `StartProtection` once during `OnStarted` to emulate the stop-loss/take-profit
  levels defined in the MQL expert.
- English comments are included directly in the code to ease maintenance and review.
- Only one trade per day is allowed because `_canTrade` is cleared right after an order is placed and
  restored only after the configured trading hour has passed.

## Usage
1. Attach the strategy to a security and configure the parameters according to the target instrument.
2. Ensure the security has a valid `PriceStep`; it is used to convert point-based parameters into
   absolute price distances.
3. Start the strategy. It will wait until the configured hour and act on the very next completed
   candle if the open-price conditions are met.
