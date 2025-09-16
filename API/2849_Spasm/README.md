# Spasm Strategy

## Summary
- Conversion of the MetaTrader 5 expert advisor *Spasm (barabashkakvn's edition)* to the StockSharp high level API.
- Trades breakouts of an adaptive channel that is sized by recent volatility and flips between bullish and bearish regimes.
- Works on any instrument and timeframe supplied by the `CandleType` parameter, defaulting to one-hour candles.

## Data Preparation
1. Subscribes to the candle series defined by `CandleType` for the strategy security.
2. Builds a volatility estimator from the last `VolatilityPeriod` candles:
   - When `UseWeightedVolatility` is disabled the estimator is a simple moving average of the per-candle range.
   - When `UseWeightedVolatility` is enabled the estimator becomes a linear weighted moving average which emphasizes the latest bars.
3. The per-candle range is `High - Low` by default. If `UseOpenCloseRange` is enabled the absolute difference between open and close is used instead, reproducing the original EA's mode switch.
4. The raw average range is converted into price steps and multiplied by `VolatilityMultiplier`. The result is floored to an integer number of steps and finally multiplied back by the instrument tick size to form the breakout threshold.
5. During the first `VolatilityPeriod * 3` finished candles the strategy collects the latest highest high and lowest low along with their timestamps to decide which swing is more recent. That information seeds the initial trend state and the reference prices once enough candles are processed.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `Volume` | `1` | Order volume applied to every market entry. |
| `VolatilityMultiplier` | `5` | Multiplier applied to the averaged volatility in order to size the breakout buffer. |
| `VolatilityPeriod` | `24` | Number of candles used for the volatility averaging routine and the initial swing scan. |
| `UseWeightedVolatility` | `false` | Switches the volatility average from simple moving average to linear weighted moving average. |
| `UseOpenCloseRange` | `false` | Uses the absolute open-close move as the volatility source instead of the high-low range. |
| `StopLossFraction` | `0.5` | Fraction of the volatility threshold employed to compute the stop-loss distance. A minimum of three price steps is enforced. |
| `CandleType` | `1 hour time frame` | Candle type and timeframe used for all calculations. |

## Trading Logic
1. **Trend tracking**
   - The strategy keeps `_highestPrice` and `_lowestPrice` as the anchors of the current swing.
   - Whenever price advances by more than the current threshold above the stored high, `_highestPrice` is updated to the candle high. Analogously a drop beyond the threshold refreshes `_lowestPrice` to the candle low.
   - The boolean `_isTrendUp` stores whether the strategy is currently in the bullish (true) or bearish (false) regime.
2. **Entry rules**
   - When `_isTrendUp` is `false` (bearish regime) and the candle close exceeds `_lowestPrice + threshold`, the strategy flips to bullish mode and sends `BuyMarket(Volume + Math.Abs(Position))`. This both closes any short exposure and opens a long position equal to `Volume`.
   - When `_isTrendUp` is `true` (bullish regime) and the candle close falls below `_highestPrice - threshold`, the strategy flips to bearish mode and sends `SellMarket(Volume + Math.Abs(Position))` to reverse into a short position.
3. **Stop management**
   - Upon entering a long position the stop price is placed at `entry - max(threshold * StopLossFraction, 3 * priceStep)`.
   - Upon entering a short position the stop price is placed at `entry + max(threshold * StopLossFraction, 3 * priceStep)`.
   - If the low of a candle reaches the long stop or the high reaches the short stop the corresponding position is closed by sending a market order. Stops are disabled when `StopLossFraction` is set to zero.
4. **Risk controls and infrastructure**
   - `StartProtection()` is called during start-up so built-in risk protections become active as soon as the strategy starts.
   - The strategy only reacts to finished candles to avoid intrabar noise, mirroring the bar-by-bar recalculation of the original EA.
   - All comments and parameter names are kept in English as required.

## Differences from the MQL Version
- The original EA recalculated thresholds on every tick. In this port the logic is executed on completed candles because the high level API operates with candle subscriptions.
- Stop-loss enforcement occurs on candle data. Intrabar stop hits that reverse inside the same bar are therefore evaluated at the candle boundaries.
- Symbol properties such as spread and broker-specific stop levels are not available in the same form in StockSharp. A conservative minimum of three price steps is used when the computed stop distance is too small, reproducing the fallback from the MetaTrader implementation.

## Notes for Use
- Ensure the strategy security exposes a valid `PriceStep`. If it is not provided the code defaults the step to `1`.
- The strategy is direction-agnostic and can be used on spot, futures, or CFD instruments as long as the feed delivers the configured candles.
- No take-profit target is defined; exits occur only via regime flips or stop-loss triggers.
