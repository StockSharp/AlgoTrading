# Early Bird Range Breakout

## Overview
Early Bird Range Breakout is a C# port of the MetaTrader 4 expert advisor `earlyBird1`. The system tracks the high and low of a configurable pre-market range, applies a 14-period RSI filter to decide the trade bias, and enters on the first breakout once the regular session opens. It preserves the original expert advisor's single-trade-per-direction constraint, volatility-controlled trailing logic, and daily closing discipline.

## Strategy logic
### Range construction
* **Time window** – the range is computed between `Range Start Hour` and `Range End Hour` (after applying the DST offset logic). Every candle that intersects this window expands the high/low boundary.
* **Entry buffer** – a configurable offset in pips is added above the range high and subtracted below the range low to mimic the MetaTrader script's `±2/Fakt` breakout buffer.
* **Daily reset** – the range, entry triggers, and trade counters reset with the first finished candle of each new trading day.

### Directional filter
* **RSI on opens** – the strategy feeds the RSI with candle open prices, matching the MT4 implementation that used `iRSI(..., PRICE_OPEN)`.
* **Bias selection** – when the RSI is above 50 only the long trigger is armed; when the RSI is 50 or lower only the short trigger is active. This ensures a single directional setup per candle, just like the original EA.

### Entry rules
* **Trading session** – new positions are allowed only on business days between `Session Start` and `Session End` after the breakout range has finished forming.
* **Single attempt per side** – once a long (or short) position is opened, the corresponding side is disabled for the remainder of the day, mirroring the daily trade counters in the MT4 code.
* **Hedging switch** – with `Allow Hedging` enabled, the strategy can reverse from a short to a long (or vice versa) by submitting enough volume to flatten the existing exposure and immediately flip direction. When hedging is disabled, entries are skipped unless the position is flat.

### Exit rules
* **Fixed risk and target** – stop-loss and take-profit levels are expressed in pips. The profit target is automatically constrained by the stop distance and by the range width, reproducing the `MathMin` logic from the original expert advisor.
* **Volatility-driven trailing** – once the current candle's range exceeds the 16-period average range multiplied by `Trailing Risk`, and the trade is in profit by at least `Trailing Trigger`, the stop is trailed by the full stop distance while the take-profit is pulled closer (half of the trailing trigger), matching the behaviour of `OrderModify` in the MQL code.
* **Session close out** – at the configured closing hour profitable trades are closed immediately. Losing positions move their take-profit to the entry price, just like the MT4 break-even enforcement.

## Parameters
* **Auto Trading** – master enable switch for automated entries.
* **Allow Hedging** – enables reversing into the opposite direction even when a position is already open.
* **Trade Direction** – limits the strategy to long only (`1`), short only (`2`), or both directions (`0`).
* **Volume** – order volume for market entries.
* **Take Profit (pips)** – maximum distance for the profit target; the effective distance is capped by the stop-loss and the range width.
* **Stop Loss (pips)** – fixed protective stop distance in pips.
* **Trailing Trigger (pips)** – minimum favourable excursion required before the trailing logic can adjust the stop and take-profit.
* **Trailing Risk** – multiplier applied to the 16-period average candle range when assessing whether volatility is high enough to trail.
* **Entry Buffer (pips)** – pip offset applied to the range boundaries when calculating breakout levels.
* **Session Start Hour / Minute** – start of the active trading window (chart time before DST adjustment).
* **Session End Hour** – end of the trading window for new positions.
* **Closing Hour** – hour after which positions are forced to break even or closed.
* **Range Start Hour / Range End Hour** – hours that define the pre-session range used for breakouts.
* **Summer Time Start / Winter Time Start** – day-of-year markers used to switch between one- and two-hour offsets, imitating the `Sommerzeit/Winterzeit` logic.
* **RSI Length** – number of periods for the RSI filter (default 14).
* **Candle Type** – primary timeframe that drives calculations (defaults to 15-minute candles).

## Additional notes
* Pip size is derived from the current price level (≥ 10 units → `0.01`, otherwise `0.0001`) exactly like the `Fakt` calculation in the MT4 script.
* Trailing statistics use the last 16 finished candles, excluding the current bar, matching the original averaging logic.
* The StockSharp strategy uses net positions, so simultaneous long and short positions are emulated by over-buying or over-selling the existing exposure when hedging is enabled.
* Only the C# implementation is provided; no Python version accompanies this strategy.
