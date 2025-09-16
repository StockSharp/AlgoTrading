# Daily Trend Reversal

## Overview
Daily Trend Reversal is a port of the MetaTrader 4 expert advisor `dailyTrendReversal_D1`. The strategy anchors intraday trades to the current day's open, high, and low, and only participates when both price action and the Commodity Channel Index (CCI) confirm the same directional bias. Trading is limited to a configurable GMT session, optionally halts after reaching a daily profit goal, and can exit positions immediately when the filters flip to the opposite side.

## Strategy logic
### Daily bias filters
* **Directional steps** – the strategy evaluates up to three conditions to validate the daily bias:
  1. Distance from the current price to the daily extreme must exceed a risk threshold expressed in pips.
  2. The distance from the open to the opposite extreme must also exceed the risk threshold and the price must remain within 10 pips of the daily open.
  3. (Optional) The current candle must close in the direction of the move while the price still sits within 10 pips of the daily open.
* **Range dominance** – compares the distance from the open to the high versus the open to the low. The longer side defines the active trend.
* **CCI trend** – the last three finished CCI values must be monotonically increasing (for longs) or decreasing (for shorts).

### Entry rules
* **Long entries**
  * Allowed only during the configured GMT trading window on business days.
  * The current price must be above the daily open, the directional steps must confirm an uptrend, the range dominance must favor the upside, and the CCI trend must be rising.
  * Only opens a long position if the net position is flat or short (short exposure is closed as part of the reversal to long).
* **Short entries**
  * Mirrored conditions: price below the daily open, directional steps confirm a downtrend, range dominance favors the downside, and CCI trend is declining.
  * Only opens when the net position is flat or long.

### Exit rules
* **Fixed take profit / stop loss** – expressed in pips relative to the entry. A value of `0` disables the respective level.
* **Session and holding control** – once the GMT closing hour is reached, or the holding time in hours elapses, profitable positions close immediately. Losing trades switch into a break-even mode and close as soon as the price returns to the entry.
* **Reversal exit (optional)** – if enabled, longs are closed when the downward filters align (price below the open and daily/CCI trends pointing lower); shorts are closed symmetrically when the upward filters align.
* **Daily profit stop** – combines realized profit since the day’s first trade with floating PnL. When the configured threshold is reached, all positions are closed and new entries are suspended until the parameter is manually re-enabled.

## Parameters
* **Auto Trading** – toggles whether the strategy may open new trades.
* **Reversal Exit** – enables immediate exits when the opposite daily trend is confirmed.
* **Trend Steps** – selects how many step filters (1–3) must pass to validate the daily bias.
* **Volume** – order volume for market entries.
* **Take Profit (pips)** – fixed profit target distance; set to `0` to disable.
* **Stop Loss (pips)** – protective stop distance; set to `0` to disable.
* **Profit Stop** – profit target in price units that pauses trading for the rest of the day; `0` disables the feature.
* **GMT Diff** – chart time minus GMT (in hours). Used to convert GMT session boundaries into chart time.
* **Start Hour / End Hour** – GMT hours that bound the trading window for new positions.
* **Closing Hour** – GMT hour after which the strategy forces exits or arms the break-even logic.
* **Holding Hours** – maximum amount of time a trade may remain open before the session logic triggers.
* **Risk (pips)** – pip distance used by the directional steps.
* **CCI Period** – number of periods for the Commodity Channel Index.
* **Candle Type** – timeframe that drives calculations (default: 15-minute candles).

## Additional notes
* The strategy detects pip size from the security’s price step. Five-digit and three-digit FX symbols automatically convert the configured pip distances to price increments.
* Daily profit tracking resets with the first candle of each new trading day by capturing the current realized PnL as the new baseline.
* There is no Python implementation for this strategy; only the C# version is provided in the API package.
