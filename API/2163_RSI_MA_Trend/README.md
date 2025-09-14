# RSI MA Trend Strategy

This strategy combines the Relative Strength Index (RSI) with a moving average trend filter.
A long position is opened when the RSI drops below a specified buy level while the fast moving average is above the slow moving average. 
A short position is opened when the RSI rises above a specified sell level while the fast moving average is below the slow moving average.

## Parameters

- `RSI Period` – length of the RSI indicator.
- `RSI Buy Level` – RSI value below which a long position is opened.
- `RSI Sell Level` – RSI value above which a short position is opened.
- `Fast MA Period` – period of the fast moving average.
- `Slow MA Period` – period of the slow moving average.
- `Candle Type` – candle series used for calculations.

## Logic

1. Subscribe to the selected candle series.
2. Calculate RSI, fast MA, and slow MA for each finished candle.
3. Detect uptrend when fast MA is above slow MA and downtrend when it is below.
4. Enter long when RSI < buy level and trend is up, closing short positions if any.
5. Enter short when RSI > sell level and trend is down, closing long positions if any.

## Notes

- The strategy uses market orders for entries.
- Trade signals are processed only on finished candles.
- Parameters are exposed for optimization in the user interface.
