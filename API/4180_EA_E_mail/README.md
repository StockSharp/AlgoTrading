# EA E-mail Strategy

## Overview
This module recreates the behaviour of the MetaTrader expert advisor *EA_E-mail*. The original code periodically sent an account
summary by e-mail without analysing market data. The StockSharp port keeps the same objective: after the strategy starts it emits
an immediate account snapshot and then continues to publish updates every `TimeIntervalMinutes` minutes. Instead of invoking
MetaTrader's `SendMail` function, the information is written to the strategy log so that hosting applications can route it to an
actual notification channel if desired.

## Conversion highlights
- A high-level candle subscription acts as the periodic timer. A synthetic series with the requested interval is created and the
  handler fires whenever a candle closes, mirroring the sleep loop from the MQL version.
- Account balance, equity and open order statistics are fetched from `Strategy.Portfolio` and the internal order tracker to
  mirror the content of the MQL mail body.
- Additional account metrics such as used margin or leverage are retrieved via reflection when the connected adapter exposes
  them; otherwise the strategy reports `N/A`, making the limitation explicit to the operator.
- The subject/body format is preserved so downstream tooling that expects the original strings can be reused.

## Parameters
| Parameter | Type | Description |
| --- | --- | --- |
| `TimeIntervalMinutes` | `int` | Interval in minutes between successive account summaries. Must be positive. |

## Reported fields
The body of each log entry contains the following lines:
- `Date and Time` – timestamp of the report in ISO 8601 format. Uses the strategy `CurrentTime` when available.
- `Balance` – initial or current account value (`Portfolio.BeginValue` fallback to `Portfolio.CurrentValue`).
- `Used Margin` – result of the `BlockedValue` or `Margin` properties if exposed by the connector; otherwise `N/A`.
- `Free Margin` – difference between balance and used margin when the previous metric is available.
- `Equity` – `Portfolio.CurrentValue`.
- `Open Orders` – count of orders in `None`, `Pending`, or `Active` state tracked internally.
- `Broker` – connector name/identifier discovered through reflection.
- `Leverage` – connector provided leverage value when available.

## Execution flow
1. `OnStarted` sends an initial summary immediately to match the original EA behaviour.
2. The strategy subscribes to a candle series whose timeframe equals `TimeIntervalMinutes` to create a scheduler without custom
   timers.
3. Every finished candle triggers a new account report. The handler assembles the message and logs it.
4. Order changes keep the active order set in sync so the open-order count remains accurate.

## Usage checklist
1. Assign a security to the strategy (required for the internal candle subscription) and choose the desired portfolio.
2. Configure `TimeIntervalMinutes` to the number of minutes between e-mails.
3. Start the strategy. Check the log output for entries beginning with `[Email] Subject:` and `[Email] Body:`.
4. Forward these log messages to an SMTP service, notification hub, or dashboard if external delivery is required.

## Limitations and notes
- Without an assigned security the periodic timer cannot start; in that case only the initial report is produced and a warning is
  logged.
- Margin-related values depend on what the connected adapter exposes through `Portfolio`. When they are absent the output shows
  `N/A` instead of zero to avoid misleading users.
- The strategy does not submit or cancel orders; it is intended purely as an informational reporting component.
