# Expert MACD EURUSD 1 Hour Strategy

## Overview

This strategy is a C# translation of the MetaTrader 5 expert advisor **Expert MACD EURUSD 1 Hour**. It trades on one-hour candles using the MACD indicator with short, long, and signal periods of **5 / 15 / 3**. The strategy looks for a strong momentum shift where the MACD main line crosses above or below the zero level while the signal line confirms the move. A trailing stop is used to protect open positions, and trades are closed when the MACD slope turns against the current position.

## Parameters

- `FastLength` – fast EMA period for MACD (default: 5).
- `SlowLength` – slow EMA period for MACD (default: 15).
- `SignalLength` – signal line period for MACD (default: 3).
- `TrailingPoints` – trailing stop distance in price points (default: 25).
- `CandleType` – timeframe of candles (default: 1 hour).
- Strategy `Volume` property controls the order size.

## Trading Logic

### Long Entry
1. Signal line values: `mac8 > mac7 > mac6` and `mac6 < mac5` (rising signal line).
2. Main line values: `mac4 > mac3 < mac2 < mac1` (main line rising after a dip).
3. `mac2 < -0.00020`, `mac4 < 0` and `mac1 > 0.00020` – main line crosses above zero.
4. If all conditions hold and no long position is open, buy at market.

### Short Entry
1. Signal line values: `mac8 < mac7 < mac6` and `mac6 > mac5` (falling signal line).
2. Main line values: `mac4 < mac3 > mac2 > mac1` (main line falling after a peak).
3. `mac2 > 0.00020`, `mac4 > 0` and `mac1 < -0.00035` – main line crosses below zero.
4. If all conditions hold and no short position is open, sell at market.

### Exit Rules
- Close a long when the current main value is below the previous one.
- Close a short when the current main value is above the previous one.
- Trailing stop updates on every candle and exits if price crosses the stop level.

## Notes

This example demonstrates using the high-level StockSharp API with indicator binding and manual trailing stop management. It is intended for educational purposes and does not include money management beyond the fixed `Volume` parameter.
