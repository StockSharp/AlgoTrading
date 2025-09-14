# RMACD Reversal Strategy

## Overview
This strategy uses the Moving Average Convergence Divergence (MACD) indicator to generate reversal signals. Four different modes define how entries are detected:

1. **Breakdown** – enters long when the MACD histogram crosses below zero and enters short when it crosses above zero.
2. **MacdTwist** – looks for a change in MACD direction by comparing the last two histogram values.
3. **SignalTwist** – monitors the signal line for direction changes.
4. **MacdDisposition** – enters when the MACD histogram crosses the signal line.

The strategy always uses market orders and reverses positions when a new opposite signal appears.

## Parameters
- **Fast Length** – period for the fast EMA inside MACD.
- **Slow Length** – period for the slow EMA inside MACD.
- **Signal Length** – smoothing period for the signal line.
- **Candle Type** – timeframe of candles used for calculations.
- **Mode** – selects the entry algorithm described above.

## Notes
- Signals are evaluated only on finished candles.
- The strategy stores previous MACD values internally instead of requesting historical data.
- No explicit stop loss or take profit is used; positions are closed only on opposite signals.
