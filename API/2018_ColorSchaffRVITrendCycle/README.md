# Color Schaff RVI Trend Cycle Strategy

This strategy implements the Color Schaff RVI Trend Cycle using the StockSharp high level API. The indicator applies a double stochastic process to the difference between fast and slow Relative Vigor Index values and smooths the result.

## Parameters
- `FastRviLength` – period for the fast RVI calculation (default 23).
- `SlowRviLength` – period for the slow RVI calculation (default 50).
- `CycleLength` – length of the stochastic cycles (default 10).
- `HighLevel` – upper threshold used to detect bullish conditions (default 60).
- `LowLevel` – lower threshold used to detect bearish conditions (default -60).
- `CandleType` – candle type processed by the strategy (default 4‑hour time frame).

## Trading Logic
1. Compute fast and slow RVI values.
2. Build the Schaff Trend Cycle from the RVI difference.
3. **Buy** when the STC value is above the high level and rising.
4. **Sell** when the STC value is below the low level and falling.

## Notes
- The strategy processes only finished candles.
- Position protection is enabled on start.
- This example is provided for educational purposes and is not financial advice.
