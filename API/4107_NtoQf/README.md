# NTOqF Multi-Filter

## Overview
The NTOqF Multi-Filter strategy ports the MetaTrader 4 expert advisor "NTOqF" (versions V1–V3) to StockSharp's high-level API. The original robot combines multiple oscillators and trend-following filters, each of which can be enabled or disabled independently. This C# version preserves the same configurability, supports separate timeframes for every indicator, and applies trade management through fixed stops, take-profit targets, and an optional trailing stop expressed in pips.

## Strategy logic
### Indicator filters
* **RSI filter** – generates a long signal when the RSI value (at the configured shift) is below `RSI Lower` and a short signal when the value is above `RSI Upper`. Neutral readings cancel entries.
* **Stochastic filter** – compares %K and %D. When `Use Stochastic High/Low` is enabled the main line must also be above `Stoch High` for longs or below `Stoch Low` for shorts; otherwise simple %K/%D crossovers are used.
* **ADX filter** – uses +DI versus –DI to determine direction. When the `Use ADX Main` option is enabled the ADX main line must exceed `ADX Main` before any entries are accepted.
* **Parabolic SAR filter** – interprets the SAR value relative to the close of the selected bar. Values above price favour longs (mirroring the behaviour in the MQL code), values below favour shorts.
* **Moving-average filter** – compares the selected moving average (with optional positive shift) to the close price at the base shift. Price above the MA favours longs; price below favours shorts.

All enabled filters must agree on the same direction. If any filter returns a neutral state (for example RSI remaining between its thresholds) no position is opened.

### Entry rules
* Signals are evaluated on the primary trading timeframe (`Candle Type`).
* Only one position is allowed at a time; the strategy waits for the previous position to close before entering a new one.
* The order volume is taken from `Trade Volume` (lots).

### Exit rules
* **Fixed stop loss / take profit** – expressed in pips and converted into price offsets using the instrument's step size. Set a parameter to `0` to disable the corresponding level.
* **Trailing stop** – when enabled, the stop is trailed once the unrealised profit exceeds the trailing distance and the current stop lags price by more than that distance. Long positions move the stop upwards, short positions move it downwards.

### Multi-timeframe behaviour
Each indicator can subscribe to its own timeframe. A timeframe value of `0` reuses the primary trading timeframe, while positive values represent minute-based `TimeFrameCandle` subscriptions. Indicator values are evaluated on completed candles only and respect the `Shift` parameter so that the strategy can mirror the "look-back" behaviour from the original MetaTrader expert.

## Parameters
* **Candle Type** – trading timeframe used to drive executions.
* **Volume** – market order volume (lots).
* **Take Profit (pips)** – profit target; `0` disables.
* **Stop Loss (pips)** – protective stop; `0` disables.
* **Use Trailing** / **Trailing Stop (pips)** – enable and size the trailing stop.
* **Shift** – number of completed candles back when reading indicator values and price.
* **RSI parameters** – toggle, period, upper/lower thresholds, and timeframe.
* **Stochastic parameters** – toggle, %K/%D/Slowing lengths, optional high/low confirmation levels, and timeframe.
* **ADX parameters** – toggle, period, DI timeframe, optional main-line threshold, and main timeframe.
* **Parabolic SAR parameters** – toggle, acceleration step, maximum acceleration, and timeframe.
* **Moving-average parameters** – toggle, period, additional shift applied to the MA buffer, averaging method (SMA/EMA/SMMA/LWMA), applied price, and timeframe.

## Notes
* Indicator queues honour the configured `Shift`, ensuring signals are based on historical values in the same way as the MQL expert.
* The trailing logic only activates once the trade is already in profit by more than the trailing distance and the stop is more than that distance away from price, matching the original EA's behaviour.
* No Python version is provided for this strategy package.
