# Tester v0.14 Strategy

This sample strategy is a simplified port of the MQL4 "Tester v0.14" script originally designed for EURUSD on the H4 timeframe.

## Logic

- Calculates a 14-period simple moving average and MACD.
- Generates a buy signal when the close price is above the SMA and MACD is positive.
- Generates a sell signal when the close price is below the SMA and MACD is negative.
- After an order is opened, the position is closed after a configurable number of bars.

This port uses the high-level StockSharp API, relying on `SubscribeCandles` and `Bind` to receive indicator values.

## Parameters

- **MinSignSum** – minimum number of signals required to open a position.
- **Risk** – percentage of account balance used for money management.
- **TakeProfit / StopLoss** – fixed levels in points.
- **BarsNumber** – number of bars to keep a position open.
- **CandleType** – candle series used (default: 4H).

## Notes

The original MQL file contained hundreds of rule combinations. This C# example illustrates the structure using a reduced rule set for clarity.
