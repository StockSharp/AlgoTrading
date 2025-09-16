# Time Trader Strategy

This strategy submits market orders at a predefined time and protects them with fixed stop loss and take profit levels.

## Trading Rules

- When the current time reaches `Trade Hour:Trade Minute:Trade Second`, the strategy fires once per session.
- If `Allow Buy` is enabled, a long position is opened with the specified `Volume`.
- If `Allow Sell` is enabled, a short position is opened with the same `Volume`.
- Protective orders are managed via `StartProtection` using point values for stop loss and take profit.

## Parameters

| Name | Description |
| ---- | ----------- |
| `Volume` | Order size. |
| `Take Profit (ticks)` | Take profit distance from entry in ticks. |
| `Stop Loss (ticks)` | Stop loss distance from entry in ticks. |
| `Allow Buy` | Enable long trades. |
| `Allow Sell` | Enable short trades. |
| `Trade Hour` | Hour of the day to trade (0-23). |
| `Trade Minute` | Minute of the hour to trade (0-59). |
| `Trade Second` | Second of the minute to trade (0-59). |
| `Candle Type` | Candle series used to track time, default is 1-second candles. |

## Notes

The strategy opens trades only once per run. To trade again, restart the strategy or adjust the trade time.
