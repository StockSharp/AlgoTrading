# Zero Lag MACD Crossover Strategy

This strategy replicates the **ZeroLagEA-AIP** algorithm from MetaTrader 5. It uses a zero lag MACD constructed from two zero lag exponential moving averages. The system opens a short position when the MACD value increases compared to the previous bar and opens a long position when the MACD decreases. If an opposite signal appears while a position is open, the current position is closed and a new one is opened on the following bar.

## Logic

1. Two zero lag EMAs with configurable periods are calculated.
2. Their difference multiplied by 10 forms the zero lag MACD value.
3. A trade is executed only when the MACD direction changes between two consecutive bars (optional).
4. Trading is allowed only between the configured start and end hours. All positions are force closed outside this window or on the specified weekday and hour.

## Parameters

- **Volume** – order volume.
- **Fast EMA** – period of the fast zero lag EMA.
- **Slow EMA** – period of the slow zero lag EMA.
- **Use Fresh Signal** – if enabled, trades only on a new MACD direction change.
- **Start Hour / End Hour** – trading session boundaries in UTC.
- **Kill Day / Kill Hour** – day of week and hour when all positions are closed.
- **Candle Type** – candle data used for calculations.

## Notes

The strategy uses high-level StockSharp API with `SubscribeCandles` and `Bind` to receive indicator values. Positions are closed using market orders.
