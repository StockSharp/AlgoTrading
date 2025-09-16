# Order Notify Strategy

## Overview
The Order Notify strategy is a utility algorithm that mirrors the MetaTrader expert advisor `OrderNotify.mq4`. Instead of running trading logic, it continuously observes the strategy's own trades and produces detailed notifications whenever a new position is opened or an existing position is closed. Notifications are written to the strategy log and can optionally be forwarded by email through an SMTP server.

The strategy is useful when you want timely updates about trading activity without watching the terminal. Each notification includes:

- The instrument symbol, trade direction, execution volume, and price.
- For closing trades, the average entry price and realized profit or loss of the closed volume.
- Current portfolio information (name, balance, and total profit of the strategy session).

## Conversion Notes
- The original MQL expert used `OrdersTotal()` to detect changes in active trades and `SendMail` for alerts. In StockSharp, the conversion leverages the `OnOwnTradeReceived` callback to react to fills and the built-in logging system.
- Email delivery is handled through configurable SMTP settings. If email is disabled or not configured, notifications stay available in the log.
- Position tracking is implemented internally to emulate the MetaTrader behaviour even when partial closes or reversals happen.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| **SendEmails** | Enables or disables email notifications. | `false` |
| **SmtpHost** | Hostname of the SMTP server. | empty |
| **SmtpPort** | Port number of the SMTP server. | `25` |
| **SmtpUseSsl** | Use SSL/TLS for the SMTP connection. | `true` |
| **SmtpUser** | Login for SMTP authentication (optional). | empty |
| **SmtpPassword** | Password for SMTP authentication (optional). | empty |
| **EmailFrom** | Sender email address used in outgoing messages. | empty |
| **EmailTo** | Recipient email address that receives the notifications. | empty |
| **SubjectPrefix** | Text prepended to every email subject. | `"Order Notify: "` |

All parameters are regular `StrategyParam` instances and can be modified before starting the strategy. Leaving the SMTP related parameters blank keeps notifications inside the log only.

## Processing Flow
1. When the strategy starts it initializes the cached position size and resets the internal average price tracker.
2. Every own trade triggers `OnOwnTradeReceived`:
   - If the trade increases exposure in the same direction, the strategy treats it as a new order, updates the average entry price and emits a "New order" notification.
   - If the trade reduces exposure (partial or full close), the strategy calculates the realized profit or loss for the closed volume, sends a "Closed order" notification, and keeps or resets the average entry price according to the remaining position.
   - If the trade reverses the position, the closed part generates a close notification and the remaining volume is treated as a fresh order.
3. `SendNotification` writes the message to the log and optionally delivers it by SMTP if the configuration is complete.

## Email Configuration Example
To forward notifications by email, fill the parameters as follows:

1. Set `SendEmails` to `true`.
2. Specify `SmtpHost`, `SmtpPort`, and whether to use SSL with `SmtpUseSsl`.
3. If your SMTP server requires authentication, set `SmtpUser` and `SmtpPassword`.
4. Provide the sender address in `EmailFrom` and the recipient in `EmailTo`.
5. Optionally customize `SubjectPrefix` to identify messages in your inbox.

The strategy uses `System.Net.Mail.SmtpClient` for delivery. Any exceptions raised during sending are logged with `LogError`, so you can diagnose configuration issues easily.

## Usage Tips
- Attach the strategy to the instrument and portfolio you want to monitor; it will report on trades executed by the strategy instance.
- Combine it with other automated strategies: launch them under the same connector so their trades trigger the notification strategy.
- The log output already contains the most important details. Enable email only when remote alerts are necessary.

