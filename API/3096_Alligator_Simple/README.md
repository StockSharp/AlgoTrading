# Alligator Simple Strategy

## Overview
The Alligator Simple strategy recreates the MetaTrader "Alligator Simple v1.0" expert advisor using StockSharp's high-level API. It reads the Bill Williams Alligator indicator on finished candles and opens a position when the Lips, Teeth, and Jaw lines expand in the same direction on the previous completed bar. Every trade can optionally include pip-based stop-loss, take-profit, and trailing stop management that mirrors the original MQL implementation.

## Indicators and Data
- **Alligator lines**: three Smoothed Moving Averages (SMMA) calculated on the candle median price `(high + low) / 2` with configurable lengths and forward shifts for the Jaw, Teeth, and Lips.
- **Candles**: the strategy subscribes to a single configurable `CandleType` (one-hour candles by default) and only processes finished candles to avoid look-ahead bias.

## Trade Logic
1. **Signal evaluation**
   - Retrieve the shifted Alligator values for the previous completed candle.
   - Long signal: `Lips[t-1] > Teeth[t-1] > Jaw[t-1]`.
   - Short signal: `Lips[t-1] < Teeth[t-1] < Jaw[t-1]`.
2. **Execution**
   - Enter the market with `OrderVolume` when no position is open.
   - Only one position is held at a time; opposite signals are ignored until the current position is closed.

## Exit and Risk Management
- **Initial stop-loss**: if `StopLossPips > 0`, the strategy offsets the fill price by the pip distance converted with the instrument's price step (including the 3/5-digit pip multiplier used by MetaTrader symbols).
- **Take-profit**: when `TakeProfitPips > 0`, a profit target is placed symmetrically around the entry price. A zero value disables the target.
- **Trailing stop**: when both `TrailingStopPips` and `TrailingStepPips` are positive, the stop is advanced to `close − TrailingStop` (longs) or `close + TrailingStop` (shorts) once price has moved at least `TrailingStop + TrailingStep` in favor of the trade. Trailing updates rely on the candle high/low to simulate intrabar touches.
- **Exit handling**: stop-loss, take-profit, and trailing conditions issue market orders to flatten the position and are evaluated on every finished candle.

## Parameters
- `OrderVolume` (default **1**): trade size in lots or contracts.
- `StopLossPips` (default **100**): initial stop-loss distance in pips. Set to zero to disable.
- `TakeProfitPips` (default **100**): take-profit distance in pips. Set to zero to disable.
- `TrailingStopPips` (default **5**): trailing stop distance in pips. Zero disables trailing.
- `TrailingStepPips` (default **5**): extra pip distance that price must travel before the trailing stop advances. Must be positive when trailing is enabled.
- `JawPeriod`, `TeethPeriod`, `LipsPeriod`: SMMA lengths for the jaw, teeth, and lips (defaults 13/8/5).
- `JawShift`, `TeethShift`, `LipsShift`: forward shifts applied when reading Alligator values (defaults 8/5/3).
- `CandleType`: candle data type/time frame for calculations (default one-hour candles).

## Implementation Notes
- Pip distances automatically adapt to the security's tick size. Instruments with three or five decimals multiply the price step by ten to replicate the MetaTrader pip definition.
- Indicator history buffers store enough values to respect the configured forward shifts, eliminating manual array manipulation.
- The strategy uses `BuyMarket` and `SellMarket` helpers to submit orders, keeping the code focused on signal generation and risk handling.
