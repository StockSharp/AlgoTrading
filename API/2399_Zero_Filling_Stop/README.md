# Zero Filling Stop Strategy

This strategy moves the stop loss to the entry price (breakeven) once the position reaches a specified profit measured in points. It does not generate its own entry signals and only manages existing positions.

## Parameters

- `ZeroFillingStop` – profit in points required to move the stop loss to the entry price. Default is 500.
- `CandleType` – type of candles used for price updates. Default is the 1-minute time frame.

## Logic

1. While a position is open, the strategy calculates profit in points using the latest finished candle.
2. When the profit exceeds `ZeroFillingStop`, the stop level is moved to the position entry price.
3. If price returns to this level, the position is closed.

This approach protects gains by automatically moving the stop to breakeven after sufficient profit is achieved.
