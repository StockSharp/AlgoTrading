# X2MA Digit DM 361 Strategy

This strategy combines two moving averages with the Average Directional Index (ADX).
A long position is opened when the fast moving average is above the slow moving average and the positive directional index (+DI) is greater than the negative directional index (-DI).
A short position is opened when the fast moving average is below the slow moving average and -DI is greater than +DI.

The strategy uses percentage based stop-loss and take-profit protections. Candles for calculations are taken from the specified timeframe.

## Parameters
- **Fast MA Length** – length of the fast moving average.
- **Slow MA Length** – length of the slow moving average.
- **ADX Length** – period for Average Directional Index calculation.
- **Stop Loss %** – stop-loss size in percent of entry price.
- **Take Profit %** – take-profit size in percent of entry price.
- **Candle Type** – candle timeframe used for processing.
