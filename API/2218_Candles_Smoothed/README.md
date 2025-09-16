# Candles Smoothed Strategy

This strategy trades based on the color of smoothed candles. For each finished candle, the difference between the close and open price is passed through a moving average. When this smoothed difference changes sign, the candle "color" switches and the strategy reverses its position.

## Logic

1. Subscribe to a configurable candle series.
2. Calculate `diff = close - open` for every finished candle.
3. Smooth the `diff` using the selected moving average.
4. Determine candle color:
   - **Color 0** if `smoothed diff > 0` (close above open).
   - **Color 1** otherwise.
5. Generate signals:
   - **Buy** when color changes from 0 to 1.
   - **Sell** when color changes from 1 to 0.
6. The current position is closed before entering a new one.

## Parameters

- `CandleType` – timeframe of the processed candles. Default is 1 hour.
- `MaLength` – length of the smoothing moving average. Default is 30.
- `MaMethod` – moving average algorithm: `Simple`, `Exponential`, `Smma`, or `Weighted`. Default is `Weighted`.

## Notes

- The strategy uses market orders via `BuyMarket` and `SellMarket`.
- High-level API is used for candle subscription and chart visualization.
- Indicator values are accessed through `TryGetValue` to avoid direct buffer calls.
