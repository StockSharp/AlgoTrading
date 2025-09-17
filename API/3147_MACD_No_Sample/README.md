# MACD No Sample

## Overview
MACD No Sample is a port of the MetaTrader 5 expert advisor `MACD No Sample`. The strategy combines a moving-average slope check with MACD signal line crossovers while enforcing a minimum MACD amplitude expressed in pips. When a bullish setup is confirmed, existing short exposure is closed before entering long; bearish setups do the opposite. Risk management mirrors the original EA with pip-based stop-loss, take-profit, and trailing logic, plus an optional risk-percentage position sizing mode.

## Strategy logic
### Indicator preparation
* **Moving average filter** – a configurable moving average (SMA, EMA, SMMA, or LWMA) applied to a selectable candle price (close, open, high, low, median, typical, or weighted). The slope (`MA[0] > MA[1]` or `<`) defines trend direction.
* **MACD signal** – MACD is calculated from independent fast/slow EMA lengths and signal length, using the chosen applied price. The raw MACD and signal lines are monitored to detect fresh crossovers and the absolute MACD magnitude is compared against a pip-based threshold.

### Entry rules
* **Long entries**
  * Moving average is rising on the latest finished candle.
  * MACD is below zero but has just crossed above the signal line (current MACD > current signal while previous MACD < previous signal).
  * Absolute MACD value exceeds the configured pip threshold (converted to price units via the detected pip size).
  * Existing short positions are closed before a long order is placed.
* **Short entries**
  * Moving average is falling on the latest finished candle.
  * MACD is above zero but has just crossed below the signal line (current MACD < current signal while previous MACD > previous signal).
  * Absolute MACD value exceeds the pip threshold.
  * Existing long positions are closed before a short order is placed.

### Exit management
* **Fixed stop-loss / take-profit** – optional pip distances converted to price offsets from the entry price. Setting either parameter to `0` disables the corresponding level.
* **Trailing stop** – activates when the trailing stop distance is positive. The strategy tracks the best price achieved since entry, shifting the stop by at least the trailing step distance (both expressed in pips) while never loosening it.
* **Risk-based sizing (optional)** – when enabled, the order volume is derived from the portfolio value, the stop-loss distance, and the configured risk percentage. Volumes are aligned to the security’s `VolumeStep`, and constrained by `MinVolume`/`MaxVolume` when available.

## Implementation notes
* Uses the high-level API through `SubscribeCandles()` with a manual indicator pipeline inside the `ProcessCandle` callback; no indicator `GetValue` calls are used.
* Indicator inputs honor the applied price selections and rely on StockSharp’s moving-average and MACD indicator implementations.
* Pip size detection mirrors the original EA logic by multiplying the price step by ten on three- and five-digit instruments.
* Stop and trailing logic closes the position via market orders when the calculated levels are breached; no separate stop orders are registered.
* Only the C# implementation is provided; there is no Python version for this strategy.

## Parameters
* **Volume** – fixed trade volume for market orders.
* **Stop Loss (pips)** – protective stop distance; `0` disables it.
* **Take Profit (pips)** – profit target distance; `0` disables it.
* **Trailing Stop (pips)** – trailing distance; `0` disables trailing.
* **Trailing Step (pips)** – minimal pip improvement before the trailing stop is adjusted.
* **Position Sizing** – choose between fixed volume and risk-percent sizing.
* **Risk Percent** – portfolio percentage used when risk sizing is active.
* **MA Period / Method / Price** – configuration for the moving-average filter.
* **MACD Fast / Slow / Signal** – EMA lengths for MACD.
* **MACD Price** – applied price used for the MACD calculation.
* **MACD Level (pips)** – minimal absolute MACD magnitude to validate a trade.
* **Candle Type** – timeframe driving the indicator updates.
