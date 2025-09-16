# JSatl Digit System Strategy

This example demonstrates a simplified port of the MQL5 "JSatl Digit System" expert advisor to StockSharp.

The strategy uses the Jurik Moving Average (JMA) to create a digital trend state:

- When the close price is above the JMA, the state becomes **up**.
- When the close price is below the JMA, the state becomes **down**.

If the state changes to up, short positions can be closed and/or a long position can be opened depending on the parameters. When the state changes to down, long positions can be closed and/or a short position can be opened.

**Parameters**

- `JmaLength` – JMA period.
- `CandleType` – candle series used for calculations.
- `StopLossPercent` – protective stop loss in percent.
- `TakeProfitPercent` – protective take profit in percent.
- `BuyPosOpen`, `SellPosOpen`, `BuyPosClose`, `SellPosClose` – enable or disable actions for corresponding signals.
