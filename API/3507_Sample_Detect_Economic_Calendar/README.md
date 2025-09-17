# Sample Detect Economic Calendar Strategy

## Overview
The **Sample Detect Economic Calendar Strategy** replicates the behaviour of the original MetaTrader expert advisor `SampleDetectEconomicCalendar.mq5`. The strategy watches a manually provided list of economic calendar events, and—when a high impact event is approaching for the configured currency—places a symmetric pair of stop orders around the current bid/ask prices. Protective stops, optional take profit levels, and a trailing exit replicate the money-management logic from the source code.

Unlike the MQL version, the StockSharp port does not have access to the MetaTrader calendar service. Instead, events are supplied by the user through the `CalendarDefinition` parameter.

## How it works
1. The strategy subscribes to Level1 data to track bid/ask prices.
2. Calendar lines defined in `CalendarDefinition` are parsed on start-up.
3. For each high importance event matching the `BaseCurrency`, the strategy:
   - Waits until `LeadMinutes` before the release.
   - Calculates the order volume (either fixed or risk-based).
   - Places buy/sell stop orders at `BuyDistancePoints` and `SellDistancePoints` from the current prices.
4. After the release, pending orders are cancelled once `PostMinutes` elapse or after the total `ExpiryMinutes` timeout.
5. When one side is triggered the opposite order is cancelled. The open position is managed with stop loss, optional take profit, and trailing stop distances expressed in points.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `TradeNews` | Enables placing pending orders around scheduled news events. |
| `OrderVolume` | Fixed order volume used when money management is disabled. |
| `StopLossPoints` | Stop-loss distance in instrument points. Set to 0 to disable. |
| `TakeProfitPoints` | Take-profit distance in points. Set to 0 to disable. |
| `TrailingStopPoints` | Trailing stop distance in points. Set to 0 to disable trailing. |
| `ExpiryMinutes` | Maximum lifetime of pending orders after the release. |
| `UseMoneyManagement` | If enabled, the volume is calculated from balance risk. |
| `RiskPercent` | Percentage of portfolio capital risked per trade (used only when money management is active). |
| `BuyDistancePoints` | Offset above the ask for the buy stop entry. |
| `SellDistancePoints` | Offset below the bid for the sell stop entry. |
| `LeadMinutes` | Minutes before release when pending orders are submitted. |
| `PostMinutes` | Minutes after release before unattended orders are cancelled. |
| `BaseCurrency` | Currency code that must appear in the calendar entry (default `USD`). |
| `CalendarDefinition` | Multiline string containing calendar events. |

## Calendar definition format
Provide one event per line in the following format:

```
yyyy-MM-dd HH:mm;CUR;High;Event title
```

* `yyyy-MM-dd HH:mm` — timestamp in UTC. Seconds are optional. Multiple date formats (`yyyy/MM/dd`, `dd.MM.yyyy`) are also supported.
* `CUR` — currency code (e.g. `USD`). Only events matching `BaseCurrency` are traded.
* `High` — importance keyword (`High`, `Medium`, `Low`, or `Nfp`). Only `High` triggers trades.
* `Event title` — free text for logging.

Example:

```
2024-06-12 18:00;USD;High;FOMC Statement
2024-07-05 12:30;USD;Nfp;Non-Farm Payrolls
```

## Risk management
* When `UseMoneyManagement` is **off**, orders are placed using the `OrderVolume` parameter.
* When `UseMoneyManagement` is **on**, the strategy risks `RiskPercent` of the portfolio value using the configured `StopLossPoints`. Exchange volume limits (min/max step) are respected.
* Trailing logic mirrors the original EA: the stop-loss and take-profit exits are enforced, and once price moves favourably by `TrailingStopPoints`, the trailing stop protects the trade.

## Differences from the MQL expert advisor
* Economic calendar events must be provided manually in `CalendarDefinition`.
* Only one instrument/currency pair is processed per strategy instance.
* Pending order expiration is handled internally with `PostMinutes`/`ExpiryMinutes` timers because StockSharp stop orders do not expose MetaTrader-style `ORDER_TIME_SPECIFIED` flags.

## Usage notes
1. Configure the `CalendarDefinition` lines before starting the strategy.
2. Enable `TradeNews` and set the desired risk parameters.
3. Ensure Level1 data is available so that bid/ask updates arrive before the news window.
4. Review the logs to confirm orders are placed and cancelled as expected around each event.
