# RndTrade Strategy

Conversion of the original MQL4 "RndTrade" expert advisor into a StockSharp high-level strategy that performs fully random market entries and exits them after a fixed holding period.

## Core Logic

1. Subscribe to the configured candle type (1-minute candles by default) and wait for completed bars.
2. Whenever the strategy is flat, generate a random number. A value above 0.5 triggers a market buy, otherwise a market sell, both using the configured trade volume.
3. Record the candle time of the entry and keep the position open for the selected holding duration (four hours by default).
4. After the holding timer elapses, close the entire position with the corresponding market order.

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| `CandleType` | Data type of candles that fire the random decision logic. | 1 minute candles |
| `TradeVolume` | Volume used for every random market order. | 1 |
| `HoldDuration` | Time span to keep any opened random position active before closing it. | 4 hours |

## Additional Notes

- The random generator is reseeded automatically when the strategy starts to mimic the MQL4 behavior of using the local time as a seed.
- Only market orders are used, reflecting the original expert advisor which immediately executed trades without pending orders.
- No additional indicators or historical buffers are required; the strategy only relies on the incoming candle timestamps and the internal timer.
