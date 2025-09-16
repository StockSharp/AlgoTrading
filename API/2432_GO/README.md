# GO Strategy

This strategy is a C# port of the original MetaTrader script "GO". It calculates a custom oscillator from moving averages of open, high, low and close prices and uses it to determine market direction.

## Strategy Logic

1. Four moving averages are built using the same period and method for the open, high, low and close series.
2. The *GO* value is computed on every finished candle:
   
   `GO = ((MA_close - MA_open) + (MA_high - MA_open) + (MA_low - MA_open) + (MA_close - MA_low) + (MA_close - MA_high)) * Volume`
3. When the GO value becomes positive, all short positions are closed and a new long position is opened.
4. When the GO value becomes negative, all long positions are closed and a new short position is opened.
5. Only one trade per bar is allowed. New entries are taken until the total number of open positions reaches **Max Positions**.

## Parameters

- **Risk %** – percentage of account equity used to calculate trade volume.
- **Max Positions** – maximum number of open positions allowed in one direction.
- **MA Type** – type of moving average (SMA, EMA, DEMA, TEMA, WMA, VWMA).
- **MA Period** – period for all moving averages.
- **Candle Type** – candle series used for indicator calculations.

## Notes

The implementation uses the high-level API of StockSharp. It subscribes to candles, binds indicators and draws them on the chart. The trade volume is adjusted according to the specified risk percentage and the security volume limits.
