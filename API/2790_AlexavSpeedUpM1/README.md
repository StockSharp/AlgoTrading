# Alexav SpeedUp M1 Strategy

## Overview
- Conversion of the "Alexav SpeedUp M1" MetaTrader 5 expert advisor to the StockSharp high-level API.
- Designed for fast markets on the 1-minute timeframe (default) and reacts to unusually large candle bodies.
- Opens a single net position in the direction of the strong candle body and manages it with fixed stop-loss, take-profit, and a stepped trailing stop.
- Uses pip-based inputs that are automatically converted to price distances according to the instrument's tick size and decimal precision.

## Original Idea vs. StockSharp Implementation
- The original EA opened both long and short trades simultaneously on hedging accounts. StockSharp strategies operate in a netting environment, so this port keeps only one position at a time and enters in the direction of the large candle.
- Trailing stop logic follows the MT5 version: it waits for price to move by `TrailingStop + TrailingStep` before moving the stop closer by the trailing distance and only updates when price advances at least one trailing step beyond the previous stop.
- Pip distances are converted to price units by multiplying by the minimum tick size. For 3 or 5 decimal Forex symbols the code multiplies the tick by 10 to emulate MT5 pip handling.

## Entry Rules
1. Work with finished candles from the selected timeframe (default: 1 minute).
2. Measure the candle body: `abs(Close - Open)`.
3. If the body exceeds `MinimumBodySizePips * pipSize` and there is no active position, enter in the direction of the candle body:
   - Bullish candle → open a long position.
   - Bearish candle → open a short position.

## Exit Rules
- **Stop-loss** – placed `StopLossPips * pipSize` away from the entry price. Disabled when the parameter is zero.
- **Take-profit** – placed `TakeProfitPips * pipSize` away from the entry. Disabled when the parameter is zero.
- **Trailing stop** – enabled when `TrailingStopPips > 0` and `TrailingStepPips > 0`.
  - Activates after the trade gains at least `TrailingStopPips + TrailingStepPips` pips.
  - For long trades, the stop is moved to `Close - TrailingStopPips * pipSize` when the condition is met and price advanced at least one trailing step beyond the previous stop.
  - For short trades, the stop is moved to `Close + TrailingStopPips * pipSize` using the same step condition.

## Parameters
- `OrderVolume` – trade size in lots (default `0.1`).
- `StopLossPips` – stop-loss distance in pips (default `30`).
- `TakeProfitPips` – take-profit distance in pips (default `90`).
- `TrailingStopPips` – trailing stop distance in pips (default `10`).
- `TrailingStepPips` – minimum favorable move before the trailing stop is updated (default `5`). Must be greater than zero when the trailing stop is enabled.
- `MinimumBodySizePips` – minimum body size (in pips) required to trigger a trade (default `100`).
- `CandleType` – timeframe for candles (default `1 Minute`).

## Visualization
- The strategy automatically draws the selected candle series and own trades in the chart area when one is available, simplifying signal inspection during testing.

## Usage Notes
- The default configuration mirrors the MT5 settings. Adjust pip distances to fit the volatility of the traded instrument.
- Because only one net position is supported, avoid running the strategy on hedging accounts expecting simultaneous long and short positions.
- For markets with larger tick sizes, reduce pip-based inputs accordingly to maintain comparable price distances.
