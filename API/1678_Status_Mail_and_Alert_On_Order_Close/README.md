# Status Mail and Alert On Order Close Strategy

This strategy monitors the account and reports important events:

- Sends a daily status notification at a specified minute.
- Reports each closed order with basic trade information.

It is based on the MQL expert *StatusMailandAlertOnOrderClose.mq4* and showcases how to handle notifications in StockSharp.

## Parameters

| Name | Description |
|------|-------------|
| `SendReportEmail` | Enable daily status notification. |
| `StatusEmailMinute` | Minute of the hour to send the status message. |
| `SendClosedEmail` | Enable notifications when orders are closed. |
| `StartBalance` | Initial account balance used for profit calculation. |
| `CandleType` | Timeframe used to check the clock. Usually set to 1 minute. |

## Logic

1. Subscribe to candles of the chosen timeframe.
2. When a candle finishes, check if it is the specified minute and send a report message.
3. On each new trade, notify if an order was closed.

These messages are logged via `AddInfo`, but can be replaced with any desired notification mechanism.
