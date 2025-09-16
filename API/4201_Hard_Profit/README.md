# Hard Profit

## Overview
Hard Profit is a StockSharp port of the MetaTrader 4 expert advisor `hardprofit.mq4`. The strategy attempts to capture breakouts
after an exhaustion move when the close finishes at the extreme of the candle and a smoothed trend filter confirms the direction.
The port rebuilds the original money management modes, staged profit taking, and stop management by using StockSharp's
high-level API.

## Strategy logic
### Breakout setup
* The strategy monitors finished candles from the configured timeframe and keeps track of the highest high and lowest low of the
  previous `Breakout Period` bars (the current candle is excluded, emulating the `iHighest`/`iLowest` call with a shift of 1).
* Median prices feed a smoothed moving average with period `Trend Period`. The slope of the moving average (current value minus
  previous value) is the directional filter used by the original EA.

### Entry rules
* **Long entries** are considered when:
  * The candle closes at its high and breaks above the previous range high.
  * The smoothed moving average slope is positive.
  * There is no open position and the trade-per-bar limit has not been hit.
  * The current spread (best ask minus best bid) is below the `Max Spread (pips)` threshold when both sides are available.
  * Long trades are not disabled by `Only Short`.
* **Short entries** mirror the above conditions: close at the low, breakout below the previous range low, negative trend slope,
  spread filter respected, and `Only Long` disabled.

### Exit management
* A fixed stop-loss (`Stop Loss (pips)`) and optional take-profit (`Take Profit (pips)`) define the outer protective envelope.
* When unrealized profit reaches `Break-even (pips)` the stop is moved to the entry price. After `Trailing Activation (pips)` the
  stop jumps ahead by the stop-loss distance, locking in profit just like the MetaTrader implementation.
* Two partial exits recycle the original percentages:
  * `Partial TP1 (pips)` closes `Partial Ratio 1 (%)` of the active position.
  * `Partial TP2 (pips)` closes `Partial Ratio 2 (%)` of the remaining position.
  The logic works on the current position volume so the second partial scales with whatever remains after the first trim.
* Stops and targets react to intrabar extremes: a long trade will exit when the candle's low breaches the stop or when the high
  touches the profit target; short trades use the symmetric conditions.

### Money management
Five sizing modes mimic the MetaTrader behaviour while accounting for StockSharp portfolio data:
1. **Fixed** – uses `Fixed Volume` on every entry.
2. **Geometrical** – scales with the square root of the portfolio value (`0.1 * sqrt(balance / 1000) * Geometrical Factor`).
3. **Proportional** – allocates a fraction of the free equity relative to the latest close (`equity * Risk Percent / (price * 1000)`).
4. **Smart** – starts from the proportional allocation and reduces the size when more than one consecutive loss is detected by
   using the `Decrease Factor` divider.
5. **TSSF** – recreates the Triggered Smart Safe-Factor logic. Average win, average loss, and win-rate are computed from the most
   recent `Last Trades` realized results. The derived metric switches between the configured `TSSF Ratio` divisors or falls back
   to a 0.1 lot minimum when conditions deteriorate. All volumes are normalized to the instrument's `VolumeStep`, `MinVolume`,
   and `MaxVolume` constraints.

## Parameters
* **Breakout Period** – number of finished candles used to compute the breakout highs and lows.
* **Trend Period** – length of the smoothed moving average applied to median price.
* **Only Short / Only Long** – directional toggles that disable the opposite side.
* **Max Trades Per Bar** – trade-per-bar guard (0 disables the limit).
* **Stop Loss (pips)** – initial stop-loss distance; set to 0 to disable.
* **Break-even (pips)** – profit threshold that moves the stop to the entry level.
* **Trailing Activation (pips)** – profit threshold that moves the stop ahead by the original stop size.
* **Partial TP1 (pips)** / **Partial Ratio 1 (%)** – distance and percentage for the first partial exit.
* **Partial TP2 (pips)** / **Partial Ratio 2 (%)** – distance and percentage for the second partial exit.
* **Take Profit (pips)** – final profit target; 0 disables the hard target.
* **Max Spread (pips)** – maximum allowed spread at the time of entry.
* **Money Management** – selects the sizing mode (Fixed, Geometrical, Proportional, Smart, TSSF).
* **Fixed Volume** – base volume when the money management mode is Fixed.
* **Geometrical Factor** – multiplier used by the geometrical sizing formula.
* **Risk Percent** – percentage of free equity used by proportional, smart, and TSSF sizing.
* **Last Trades** – number of recent realized trades stored for adaptive sizing.
* **Decrease Factor** – divider applied to the smart mode when consecutive losses occur.
* **TSSF Trigger 1/2/3 & TSSF Ratio 1/2/3** – thresholds and divisors for the TSSF metric transitions.
* **Candle Type** – primary timeframe that drives indicator updates and signal evaluation.

## Additional notes
* Pip values are derived from the security price step; five-digit FX symbols automatically map one pip to 10 points.
* Partial exits do not reset the trade-per-bar counter, replicating the MetaTrader behaviour of counting only new entries.
* Money management statistics are built from realized PnL differences, so the history becomes meaningful once the first trades
  close in the StockSharp environment.
* If best bid/ask data is unavailable the spread filter is effectively disabled, matching the behaviour of the original EA when
  the broker reported a zero spread.
