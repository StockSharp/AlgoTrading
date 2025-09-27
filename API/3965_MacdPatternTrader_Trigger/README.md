# Macd Pattern Trader Trigger Strategy

## Overview
Macd Pattern Trader Trigger Strategy ports the MetaTrader 4 expert advisor `MacdPatternTraderv05cb` to StockSharp's high-level strategy API. The system trades pure MACD histogram patterns, looking for a double-top structure below the zero line to open shorts and a mirror image double-bottom above the zero line to open longs. Trade management mirrors the original EA: every entry is submitted at market with a configurable fixed stop loss and take profit measured in instrument points.

## Strategy logic
### Indicator stream
* A single candle subscription drives the logic (default: 15-minute candles). Each finished candle feeds a `MovingAverageConvergenceDivergence` indicator configured with the unusual MT4 parameters `(fast = 13, slow = 5, signal = 1)` used by the source EA.
* Only the MACD main line is used. The strategy buffers the last three completed values in order to emulate `iMACD(..., MODE_MAIN, shift=1..3)` from MetaTrader.

### Bullish setup (long entries)
1. **Arming condition** – the MACD line must rise above `Bullish Trigger` (default `0.0015`). This prepares the strategy to look for the pullback sequence. Any dip below zero clears the state immediately.
2. **Pullback window** – once armed, the MACD has to fall back below `Bullish Reset` (default `0.0005`). This marks the potential pullback area. The window remains active until a valid pattern is confirmed or MACD turns negative.
3. **Pattern confirmation** – while the window is active, the last three buffered MACD readings must satisfy:
   * `macd_curr > macd_last` (momentum turns back up),
   * `macd_last < macd_last3` (the previous bar set the swing low),
   * `macd_curr > Bullish Reset` and `macd_last < Bullish Reset` (price rebounds from the shallow pullback zone).
4. **Execution** – when confirmed, the strategy buys at market. If there is an existing short position, the order size automatically includes the volume required to flatten before establishing the long exposure.

### Bearish setup (short entries)
1. **Arming condition** – the MACD line must drop below `-Bearish Trigger` (default `-0.0015`). Any move above zero clears all bearish state.
2. **Pullback window** – once armed, the MACD has to rebound above `-Bearish Reset` (default `-0.0005`).
3. **Pattern confirmation** – while the window is open, the buffered values must satisfy:
   * `macd_curr < macd_last`,
   * `macd_last > macd_last3`,
   * `macd_curr < -Bearish Reset` and `macd_last > -Bearish Reset`.
4. **Execution** – a market sell order is submitted. If a long position exists, its volume is included in the order so the account ends up net short by the configured trade size.

### Risk management
* **Fixed stop loss / take profit** – distances are specified in points (price steps). The strategy multiplies them by the instrument's `PriceStep` and calls `StartProtection` to reproduce the original SL/TP behaviour. Setting a distance to `0` disables the respective level.
* **One signal per window** – after placing an order, the arming and window flags are cleared to avoid repeated entries from the same MACD pattern.

## Parameters
* **Trade Volume** – market order volume. Opposite positions are closed automatically before opening the new trade.
* **Fast EMA / Slow EMA / Signal EMA** – MACD lengths. Defaults replicate the original advisor but may be optimised.
* **Bullish Trigger / Reset** – positive MACD thresholds (in indicator units) that arm the long setup and define its pullback zone.
* **Bearish Trigger / Reset** – absolute MACD thresholds for the short setup. The trigger is applied with a negative sign during runtime.
* **Stop Loss / Take Profit** – distances in points (price steps). A value of `0` disables the corresponding protection.
* **Candle Type** – timeframe used for the MACD calculation and trading decisions.

## Implementation notes
* The StockSharp high-level API is used throughout: `SubscribeCandles` feeds the indicator and `StartProtection` mirrors the MT4 trade management.
* The MACD history buffer ensures the decision logic operates on the previous three finished bars, matching MetaTrader's `shift=1..3` calls.
* There is no Python version of this strategy in the API package, only the C# implementation.
