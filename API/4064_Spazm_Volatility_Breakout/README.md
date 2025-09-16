# Spazm Volatility Breakout Strategy

## Summary
- Conversion of the MetaTrader 4 expert advisor **Spazm (8683)** to the StockSharp high level API.
- Trades adaptive breakouts by comparing the latest close against volatility-sized envelopes around the most recent swing high and swing low.
- Maintains optional chart annotations that join consecutive bullish and bearish pivots just like the original MQL visualization.

## Data Preparation
1. The strategy subscribes to the candle series specified by the `CandleType` parameter for the active security.
2. Every finished candle provides the raw range sample used for volatility estimation:
   - By default the range equals `High - Low`.
   - When `UseOpenCloseRange` is enabled the absolute body size `|Open - Close|` is used instead.
3. The range sample is converted to price steps using the instrument `PriceStep` so the logic remains invariant across symbols.
4. The indicator defined by `UseWeightedVolatility` processes the sequence of range samples:
   - Disabled → simple moving average with length `VolatilityPeriod`.
   - Enabled → linear weighted moving average (more weight to recent candles).
5. The smoothed range (expressed in steps) is multiplied by `VolatilityMultiplier` and finally scaled back to price units. The resulting value is the adaptive breakout threshold applied to both sides of the market.
6. During the warm-up phase the strategy also records the most recent extreme high and extreme low along with their timestamps. Once `VolatilityPeriod * 3` candles are processed the relative timing of those extremes determines the initial trend direction.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `Volume` | `1` | Order volume sent whenever the strategy opens or reverses a position. |
| `VolatilityMultiplier` | `5` | Multiplier applied to the averaged volatility in order to build the breakout distance. |
| `VolatilityPeriod` | `24` | Number of candles used both for the volatility estimator and for seeding the initial swing extremes. |
| `UseWeightedVolatility` | `false` | Switches the volatility estimator from a simple to a linear weighted moving average. |
| `UseOpenCloseRange` | `false` | Uses the absolute open-close move instead of the high-low range when measuring volatility. |
| `StopLossMultiplier` | `0` | Multiplier applied to the breakout threshold to compute a protective stop distance. A minimum of three price steps is enforced. Set to `0` to disable stops. |
| `DrawSwingLines` | `true` | When enabled the strategy draws a line between the latest bullish and bearish pivots, mimicking the MQL objects. |
| `CandleType` | `4 hour time frame` | Candle type (time frame or other data type) that feeds the calculations. |

## Trading Logic
1. **Initialization**
   - While the first `VolatilityPeriod * 3` candles are processed the strategy updates `_highestPrice`, `_lowestPrice`, `_highestTime`, and `_lowestTime` to capture the latest extremes.
   - After enough candles arrive the more recent of the two extremes defines the initial trend: if the last low is newer than the last high the strategy starts in bullish mode, otherwise it starts bearish.
   - The extremes are also stored as the first pair of swing anchors so chart lines can be drawn immediately after the warm-up.
2. **Volatility Tracking**
   - Each finished candle pushes its range into the selected moving average to produce the adaptive threshold.
   - The threshold is always at least one price step to avoid zero-distance envelopes.
3. **Swing Maintenance**
   - On every candle the algorithm refreshes the stored swing high and swing low whenever a new absolute high or low is printed.
   - When the trend flips the relevant extreme is recorded as a pivot and, if charting is enabled, connected with the opposite pivot by a line.
4. **Breakout Rules**
   - Bullish regime (`_isTrendUp == true`): a close below `_highestPrice - threshold` triggers a reversal to short. The order size equals `Volume + |Position|` so existing exposure is flattened and a new short position is opened in one call.
   - Bearish regime (`_isTrendUp == false`): a close above `_lowestPrice + threshold` mirrors the logic and reverses into long.
5. **Stop Management**
   - When `StopLossMultiplier` is greater than zero the entry price is offset by `threshold * StopLossMultiplier` (bounded to at least three price steps) to derive a synthetic stop level.
   - If a candle pierces the long stop with its low or the short stop with its high the position is flattened via a market order.
6. **Infrastructure**
   - `StartProtection()` enables built-in StockSharp safety mechanisms as soon as the strategy launches.
   - All actions are driven by finished candles to emulate the bar-by-bar recalculation cycle from the original expert advisor.

## Differences from the MQL Version
- The MetaTrader expert recalculates on every tick, whereas this port operates on completed candles because candle subscriptions are the idiomatic data source in the high level API.
- Broker-specific restrictions such as `MODE_STOPLEVEL` are not available; instead the stop offset is bounded by three price steps to provide a conservative fallback.
- Orders are reversed by combining the closing and opening quantities into a single `BuyMarket`/`SellMarket` call instead of iterating over existing positions.
- Visualization relies on StockSharp chart primitives (`DrawLine`) instead of platform objects, yet the arrangement of pivot-to-pivot lines matches the original indicator output.

## Notes for Use
- Ensure the selected security exposes a valid `PriceStep`. When missing, the code defaults to `1`, which may need adjustment for certain instruments.
- Because the strategy depends on completed candles, extremely small timeframes reduce the reliability of the volatility estimate. Consider aligning `CandleType` with the timeframe originally used by the EA (H4 by default).
- Stops are optional. Leaving `StopLossMultiplier` at zero replicates the uncapped risk management from the MQL script.
- The algorithm is trend-following by design and does not impose take-profit targets; exits occur only by regime reversal or stop-loss activation.
