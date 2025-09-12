# RSI Strategy with Manual TP and SL

Implements an RSI strategy that enters long when RSI crosses above the oversold level and the close is above 70% of the highest close over the last 50 candles. Enters short when RSI crosses below the overbought level and the close is below 130% of the lowest close over the last 50 candles. Positions are protected using percentage-based take profit and stop loss.

## Parameters

- **Candle Type** – candle timeframe.
- **RSI Length** – period for RSI.
- **Oversold Level** – RSI threshold for long entries.
- **Overbought Level** – RSI threshold for short entries.
- **Lookback** – period for high/low calculation.
- **Take Profit %** – take profit percentage.
- **Stop Loss %** – stop loss percentage.
