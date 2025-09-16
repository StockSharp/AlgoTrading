# Good Gbbi Strategy

This strategy opens a single position at a specific hour of the day based on the difference between historical open prices.

## Logic

* Works on hourly candles by default.
* At `TradeTime` hour the strategy compares the open price from `T1` bars ago with the open price from `T2` bars ago.
* If the older open is higher than the recent one by `DeltaShort` points a short position is opened.
* If the recent open is higher than the older one by `DeltaLong` points a long position is opened.
* Only one trade per day is allowed. Trading is enabled again after the hour is greater than `TradeTime`.
* Each position is protected by individual take-profit and stop-loss levels and can be forcibly closed after `MaxOpenTime` hours.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `TakeProfitLong` | Take profit distance in points for long positions. |
| `StopLossLong` | Stop loss distance in points for long positions. |
| `TakeProfitShort` | Take profit distance in points for short positions. |
| `StopLossShort` | Stop loss distance in points for short positions. |
| `TradeTime` | Hour of day when the entry conditions are checked. |
| `T1` | Number of bars back for the first open price. |
| `T2` | Number of bars back for the second open price. |
| `DeltaLong` | Required difference in points to open a long position. |
| `DeltaShort` | Required difference in points to open a short position. |
| `MaxOpenTime` | Maximum position holding time in hours, 0 disables the check. |
| `CandleType` | Candle type to process. |

## Notes

The original idea comes from the MetaTrader expert advisor *GoodG@bi*. This port uses StockSharp high-level API and processes only finished candles. Ensure that the security's `PriceStep` is configured correctly to interpret point values.
