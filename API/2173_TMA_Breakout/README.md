# TMA Breakout

This strategy exploits breakouts relative to a Triangular Moving Average (TMA). It watches a configurable candle series and compares the previous candle's close to the TMA value plus or minus user defined offsets. A long position is opened when the previous close is above `TMA + UpLevel`, and a short position is opened when it is below `TMA - DownLevel`. Opposite signals reverse the position.

## Parameters

- **TMA Length** – period used to calculate the Triangular Moving Average.
- **Upper Level** – price offset added to the TMA to detect long signals.
- **Lower Level** – price offset subtracted from the TMA to detect short signals.
- **Candle Type** – timeframe of candles used by the strategy.

## How it works

1. Subscribes to the selected candle series.
2. Binds a Triangular Moving Average indicator to the candles.
3. On each finished candle it:
   - Stores the previous TMA and close values.
   - Checks if the previous close exceeded the upper or lower level.
   - Sends market orders to open or reverse positions accordingly.
4. Charts candles, indicator line and own trades for visual analysis.

## Notes

The strategy uses market orders without stop-loss or take-profit management. It is intended for educational purposes and should be extended with proper risk controls before live trading.
