# Big Dog Range Breakout Strategy

The **Big Dog** strategy searches for a narrow consolidation window inside the London morning session and trades breakouts from that box. The original MQL expert advisor placed stop orders once the price range between the specified `StartHour` and `StopHour` stayed within a configurable number of points. The StockSharp port keeps the same idea and uses market orders when the breakout happens, accompanied by dynamic stop-loss and take-profit levels derived from the consolidation extremes.

## Trading Logic

1. Collect finished candles between `StartHour` (inclusive) and `StopHour` (exclusive by default) to build the daily range.
2. Ignore the session if the difference between the session high and low exceeds `MaxRangePoints` (converted into price units using the adjusted point size).
3. After the session closes, check the distance between the latest best ask/bid and the breakout levels. A setup is activated only if the market is at least `DistancePoints` away from the high (for long entries) or low (for short entries).
4. When price breaks through the prepared high or low on a subsequent candle, enter with a market order sized by `OrderVolume` (automatically offsetting any opposite position).
5. Immediately assign exits:
   - Long trades use a stop-loss at the recorded session low and a take-profit placed `TakeProfitPoints` above the entry level.
   - Short trades use a stop-loss at the recorded session high and a take-profit placed `TakeProfitPoints` below the entry level.
6. On each finished candle the strategy monitors the high/low to decide whether the stop-loss or take-profit was reached and closes the position accordingly.
7. At the beginning of a new trading day all cached levels are reset to prevent leftover orders from the previous session.

> **Adjusted points.** The strategy converts point-based inputs into actual price distances by multiplying them by the instrument `PriceStep`. When the security has 3 or 5 decimals the value is additionally scaled by 10 to mimic the pip logic used in the original EA.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `StartHour` | Hour of day (0-23) when the consolidation window starts. | `14` |
| `StopHour` | Hour of day (0-23) when the consolidation window stops. | `16` |
| `MaxRangePoints` | Maximum height of the session box measured in adjusted points. | `50` |
| `TakeProfitPoints` | Take-profit distance in adjusted points from the breakout price. | `50` |
| `DistancePoints` | Minimum distance between current price and breakout level before activating orders. | `20` |
| `OrderVolume` | Volume of each breakout trade (also applied to strategy `Volume`). | `1` |
| `CandleType` | Candle type used for building the session box. One-hour time frame by default. | `1h` |

## Implementation Notes

- The strategy subscribes to both candles and the order book. Best bid/ask values are used to evaluate the distance filters, falling back to the latest candle close if no depth is available.
- Entries are executed with market orders. This mirrors the behaviour of the original pending stop orders while staying within the high-level API.
- Stop-loss and take-profit decisions are performed on candle closes based on intra-bar highs and lows, which emulates the protective levels of the MQL version without registering extra child orders.
- Daily state management cancels any active orders and resets cached highs/lows whenever the calendar date changes.

