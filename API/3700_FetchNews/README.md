# Fetch News Strategy

## Overview
- Port of the MetaTrader 5 expert advisor *FetchNews* (NewsEA.mq5) to the StockSharp high-level API.
- Monitors a user-supplied macroeconomic calendar and reacts either by logging alerts or by placing a pending order straddle.
- Designed for discretionary traders who already curate a calendar of news releases and need automated preparation before the event.
- Uses Level1 best bid/ask quotes to anchor pending orders and relies on the built-in protective order helpers for stop-loss and take-profit management.

## Operating modes
1. **Alerting** (`Mode = Alerting`).
   - Filters events by importance (`AlertImportance`) and currency (`OnlySymbolCurrencies`).
   - Writes a journal message such as `Upcoming important news event: CPI at 2024-06-12T12:30:00Z (USD).`
   - No trading activity is performed. Use this mode when the strategy should only warn about news that match the trading instrument.
2. **Trading** (`Mode = Trading`).
   - Still respects the currency filter (`OnlySymbolCurrencies`).
   - Requires the event name to contain at least one keyword from `TradingKeywords` (semicolon, comma, or newline separated).
   - When a matching event enters the active window (`LookBackSeconds` before, `LookAheadSeconds` after), the strategy checks that:
     - There is no open position and no other pending straddle active.
     - Best bid/ask prices are known from the Level1 feed.
     - Security price step is available in order to convert MetaTrader "points" into price units.
   - Two pending orders are registered around the market price:
     - Buy Stop at `ask + TakeProfitPoints * PriceStep`.
     - Sell Stop at `bid - TakeProfitPoints * PriceStep`.
   - When either side is executed the opposite pending order is cancelled and stop-loss / take-profit orders are placed using `SetStopLoss` and `SetTakeProfit` with the configured distances (in points).
   - Pending orders automatically expire after `OrderLifetimeSeconds`; both orders are cancelled once the timer elapses or the position is closed.

## Calendar definition
`CalendarEventsDefinition` contains one event per line (line break or `;` separator). Each record must provide at least four comma-separated fields:

```
DateTime, Currency, Importance, Name
```

- **DateTime** – parsed with `DateTime.TryParse` using invariant culture. Example: `2024-06-12 12:30`.
- **Currency** – currency code that identifies the event (e.g., `USD`, `EUR`).
- **Importance** – accepted values: `Low`, `Moderate`, `High`, or synonyms (`medium`, `important`).
- **Name** – full event description. Additional commas become part of the name.

`TimeZoneOffsetHours` shifts every parsed timestamp before it is converted to UTC. For example, if the file is in Eastern Time (UTC-4 during summer) set the offset to `-4`. The strategy compares events in UTC against the server time supplied by Level1 updates.

### Example
```
2024-06-12 12:30,USD,High,Consumer Price Index (YoY)
2024-06-12 14:00,USD,Moderate,FOMC Interest Rate Decision
2024-06-13 08:00,EUR,High,ECB Press Conference
```

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `Mode` | Alerting or trading behaviour. | `Alerting` |
| `OrderVolume` | Volume in lots for each pending order. | `0.1` |
| `TakeProfitPoints` | Distance in points between entry and take-profit. | `150` |
| `StopLossPoints` | Distance in points between entry and stop-loss. | `150` |
| `OrderLifetimeSeconds` | Lifetime of pending orders in seconds. | `500` |
| `LookBackSeconds` | Seconds before event time when processing starts. | `50` |
| `LookAheadSeconds` | Seconds after event time when processing remains active. | `50` |
| `TradingKeywords` | Keywords that must appear inside the event name in trading mode. | `cpi;ppi;interest rate decision` |
| `CalendarEventsDefinition` | CSV-like list of events. | empty |
| `TimeZoneOffsetHours` | Hour offset applied to event timestamps before converting to UTC. | `0` |
| `AlertImportance` | Minimum importance for alerts in alerting mode. | `Moderate` |
| `OnlySymbolCurrencies` | Restrict events to currencies derived from the instrument code. | `true` |

## Workflow
1. `OnStarted` resets state, loads currency filters, keywords, and calendar records, then subscribes to Level1 data. `StartProtection()` enables the StockSharp protective order helpers.
2. Every Level1 update (`ProcessLevel1`) stores the latest bid/ask, cancels expired pending orders, and scans the calendar for events inside the active time window.
3. Alerting mode simply logs qualifying events once. Trading mode calls `ProcessTrading`, which validates trading conditions and registers the straddle orders.
4. `OnNewMyTrade` identifies whether the filled order was the buy or sell stop, cancels the opposite side, and immediately places stop-loss and take-profit orders for the resulting position.
5. `OnPositionChanged` clears the pending-expiration timer when the strategy becomes flat, allowing a new event to be processed.

## Differences compared to the MetaTrader version
- MetaTrader fetched the economic calendar directly from the broker. StockSharp does not provide such data out of the box, therefore events must be supplied manually through `CalendarEventsDefinition`.
- Stop-loss and take-profit distances cannot be attached directly to pending orders. Instead the strategy calls `SetStopLoss` / `SetTakeProfit` after execution using the built-in protection service.
- Symbol currency detection is heuristic: the strategy extracts three-letter segments from the instrument code (e.g., `EURUSD`, `GBP/JPY`). Adjust `OnlySymbolCurrencies` if necessary.
- Alert notifications use the logging subsystem (`LogInfo`) rather than MetaTrader pop-up alerts.

## Usage tips
- Maintain the calendar text file outside the strategy, then copy/paste its contents into `CalendarEventsDefinition` before starting the strategy.
- Keep the keyword list concise (e.g., `cpi;ppi;interest rate decision;nfp`) to focus on events with historically large price impact.
- Combine with a live data feed that supports Level1 best bid/ask updates; otherwise pending orders will not be placed.
- Test the workflow in the StockSharp simulator to ensure the protective orders behave as expected with the chosen instrument.
