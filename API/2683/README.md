# Trade EA Template for News Strategy

## Overview
Trade EA Template for News Strategy is a C# conversion of the MetaTrader 4 expert advisor "Trade EA Template for News". The original system paused trading around scheduled economic events downloaded from external websites. This StockSharp port keeps the core ideas while adapting them to the high-level API:

- Uses completed candles from the configured timeframe (H1 by default).
- Trades only when the account is flat, exactly like the MQL template that required zero open orders.
- Applies a manual economic news blackout that blocks entries before and after events depending on their importance.
- Automatically creates protective stop-loss and take-profit brackets 100 points away from the fill price (converted through the security step).

## Trading Logic
1. Each finished candle triggers a recalculation of the news schedule. The strategy stores the open price of the previous candle so that the next bar can compare its close to the prior open.
2. If the current time falls inside any configured blackout window the strategy cancels pending orders and does not open new trades.
3. When no position is open and trading is allowed:
   - A long position is opened if the latest candle closes above the previous candle's open price.
   - A short position is opened if the latest candle closes below the previous candle's open price.
4. Stop-loss and take-profit levels are expressed in points (`TakeProfitPoints` and `StopLossPoints`) and converted into absolute price offsets using the security's `Step` value.

## Manual news schedule
The original expert downloaded data from investing.com or DailyFX. For portability the StockSharp version expects a manually curated calendar supplied through the `NewsEventsDefinition` parameter. The format accepts a list of entries separated by semicolons or line breaks. Every entry must contain at least three comma-separated fields:

```
YYYY-MM-DD HH:MM,CURRENCIES,IMPORTANCE[,TITLE]
```

- `YYYY-MM-DD HH:MM` — event start in UTC. The optional `TimeZoneOffsetHours` parameter shifts all parsed times by the requested amount (for example set `3` for UTC+3).
- `CURRENCIES` — currency codes or instrument identifiers such as `USD`, `EUR`, `EUR/USD`. Multiple codes can be separated with `/`, `,`, `;`, `|` or spaces.
- `IMPORTANCE` — importance keyword. Recognised values: `Low`, `Medium`, `Mid`, `Midle`, `Moderate`, `High`, `NFP`, strings containing `Nonfarm` or `Non-farm`.
- `TITLE` — optional free text description that will be printed in log messages.

Example:

```
2024-03-01 13:30,USD,High,Nonfarm Payrolls;2024-03-01 15:00,USD,Low,Factory Orders
```

### Blackout windows
- `UseLowNews`, `UseMediumNews`, `UseHighNews` and `UseNfpNews` toggle which events are considered.
- `LowMinutesBefore/After`, `MediumMinutesBefore/After`, `HighMinutesBefore/After` and `NfpMinutesBefore/After` determine how many minutes around the event trading should be disabled.
- `OnlySymbolNews` restricts the blackout to entries whose currency codes match the current security (for example `EURUSD` results in the pair `{EUR, USD}`). Disable it to pause trading on every event.
- The strategy keeps only the highest importance event active at any given time. Informational log messages announce the reason for the current state and the next scheduled release.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `CandleType` | Candle data type to subscribe to. Defaults to 1 hour. | `1h` |
| `UseLowNews` | Enable low importance events. | `true` |
| `LowMinutesBefore` / `LowMinutesAfter` | Minutes before/after low impact news to block entries. | `15 / 15` |
| `UseMediumNews` | Enable medium importance events. | `true` |
| `MediumMinutesBefore` / `MediumMinutesAfter` | Minutes before/after medium impact news. | `30 / 30` |
| `UseHighNews` | Enable high importance events. | `true` |
| `HighMinutesBefore` / `HighMinutesAfter` | Minutes before/after high impact news. | `60 / 60` |
| `UseNfpNews` | Enable the Non-farm Payrolls flag. | `true` |
| `NfpMinutesBefore` / `NfpMinutesAfter` | Minutes before/after NFP events. | `180 / 180` |
| `OnlySymbolNews` | Filter the calendar by the current security's currency codes. | `true` |
| `NewsEventsDefinition` | Manual economic calendar description string. | empty |
| `TimeZoneOffsetHours` | Offset applied to every parsed event (UTC by default). | `0` |
| `TakeProfitPoints` | Distance in points for the protective take-profit order. | `100` |
| `StopLossPoints` | Distance in points for the protective stop-loss order. | `100` |

`Volume` is inherited from `Strategy` and should be set according to the desired position size.

## Differences from the MQL version
- No automatic HTTP download — the user supplies the news list manually, which avoids external dependencies and keeps the conversion deterministic.
- Chart labels and vertical lines are replaced with log messages that describe the active or upcoming event.
- The MQL expert opened orders with fixed lot size `0.01`; in StockSharp the position size comes from the `Volume` property.
- All logic is implemented with the high-level candle subscription API while preserving the template's news-aware behaviour.

## Deployment notes
1. Fill `NewsEventsDefinition` before starting the strategy or update it, stop and restart to reload the schedule.
2. Adjust `TimeZoneOffsetHours` and the minutes-before/after parameters to match your trading session.
3. Set `Volume`, portfolio and security in the UI or in code, then start the strategy.
4. Watch the strategy log for messages such as "Trading paused due to high news" or "Next scheduled news" to confirm the blackout logic.

Python translation is intentionally omitted as requested.
