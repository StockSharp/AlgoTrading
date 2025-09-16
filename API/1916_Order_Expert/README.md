# Order Expert Strategy (1916)

This strategy opens a market position when the instrument price reaches predefined levels. It mimics behaviour of the original MQL expert that managed orders via chart lines.

## How it works
- Subscribes to candles of a configurable timeframe.
- When the closing price crosses the `BuyLevel` or `SellLevel` thresholds it opens a long or short market position.
- Stop-loss and take-profit values are calculated from the entry price using `StopLossPip` and `TakeProfitPip`.
- Optional trailing stop moves the stop-loss towards the current price as it moves in a favorable direction.

## Parameters
- **TakeProfitPip** – distance from entry price to take profit in pips.
- **StopLossPip** – distance from entry price to stop loss in pips.
- **EnableTrailingStop** – enable or disable trailing stop logic.
- **CandleType** – candle type used for calculations.
- **BuyLevel** – price level that triggers long entry (0 disables).
- **SellLevel** – price level that triggers short entry (0 disables).

## Notes
- Strategy uses high-level API and processes only finished candles.
- Protection subsystem is activated on start to avoid accidental large positions.
