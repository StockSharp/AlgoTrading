# Sidus Alligator Strategy

## Overview
The Sidus strategy reproduces the classic MetaTrader "Sidus" expert advisor logic in StockSharp. It combines the Bill Williams Alligator indicator with a 14-period Relative Strength Index (RSI) filter. The system looks for an RSI cross above or below the 50 midline while all three Alligator moving averages expand in the same direction. Each entry immediately calculates protective stops and optional trailing management expressed in pip distances that respect the security's price step.

## Indicators and Data
- **Alligator lines**: three smoothed moving averages calculated on the candle median price (high + low ÷ 2) with independent lengths and forward shifts for jaw, teeth, and lips. Consecutive values are compared to detect upward or downward expansion.
- **Relative Strength Index (RSI)**: a 14-period oscillator evaluated on close prices. Only finished candles participate in the decision to avoid look-ahead bias.
- **Candles**: any time frame can be selected through the `CandleType` parameter. By default, the strategy uses one-minute time frame candles.

## Trade Logic
1. **RSI confirmation**
   - Long setup: RSI crosses upward through 50 (`RSI[t-2] < 50` and `RSI[t-1] > 50`).
   - Short setup: RSI crosses downward through 50 (`RSI[t-2] > 50` and `RSI[t-1] < 50`).
2. **Alligator slope filter**
   - Long entry requires the jaw, teeth, and lips slopes between the two previous completed values (taking shifts into account) to exceed the `Delta` threshold.
   - Short entry requires the same slopes to be below the threshold, indicating compression or decline.
3. **Position handling**
   - When a long signal appears, shorts are closed first if `CloseOpposite = true`. The strategy then buys the configured `OrderVolume` at market.
   - When a short signal appears, longs are flattened if allowed by `CloseOpposite`, followed by a market sell of `OrderVolume`.

## Exit and Risk Management
- **Initial stop-loss**: calculated from the previous candle extreme minus/plus `OffsetPips` (converted using the instrument's price step). Stops are skipped if the calculated level would invalidate the trade (e.g., non-positive distance).
- **Take-profit**: optional distance defined by `TakeProfitPips`. Setting the parameter to zero disables the target.
- **Trailing stop**: if `TrailingStopPips` and `TrailingStepPips` are both positive, the stop is advanced once price moves at least `TrailingStopPips + TrailingStepPips` in favor of the position. The new stop is placed `TrailingStopPips` away from the highest high (longs) or lowest low (shorts) reached during the bar.
- **Flattening logic**: Stop-loss, take-profit, and trailing logic are evaluated on every finished candle using high/low ranges to simulate intrabar touches.

## Parameters
- `OrderVolume` (default **0.1**): trade size in lots or contracts.
- `OffsetPips` (default **3**): distance from the previous candle extreme to the stop-loss. Zero disables the initial stop.
- `TakeProfitPips` (default **75**): take-profit distance. Zero disables the target.
- `TrailingStopPips` (default **5**): trailing stop distance. Must be positive if trailing is enabled.
- `TrailingStepPips` (default **15**): additional move required before the trailing stop advances. Must be positive when trailing is enabled.
- `Delta` (default **0.00003**): minimum slope difference for each Alligator line between consecutive samples.
- `CloseOpposite` (default **false**): if `true`, opposite positions are closed before opening a new trade; if `false`, the strategy waits for the current position to flatten naturally.
- `JawPeriod`, `TeethPeriod`, `LipsPeriod`: lengths of the smoothed moving averages for the Alligator jaw, teeth, and lips (defaults 13/8/5).
- `JawShift`, `TeethShift`, `LipsShift`: forward shifts (defaults 8/5/3) used when retrieving slope comparisons.
- `RsiPeriod` (default **14**): RSI averaging window.
- `CandleType`: candle data type/time frame to subscribe to (default 1 minute).

## Implementation Notes
- Pip-based distances automatically adapt to the security's price precision: five- and three-decimal instruments multiply the price step by ten to match the MQL pip definition.
- Alligator slope checks rely on stored historical values that respect the configured forward shifts, avoiding manual array management beyond a minimal ring buffer.
- Orders are executed with the high-level `BuyMarket` and `SellMarket` helpers, keeping the strategy focused on signal generation while StockSharp handles routing.
