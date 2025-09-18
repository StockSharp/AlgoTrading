# News Template Universal Strategy

## Overview
The **News Template Universal Strategy** replicates the behaviour of the original MQL expert advisor that halts trading around
scheduled economic news releases. The strategy listens to `NewsMessage` updates produced by the connected broker or data
provider and searches for events that match the configured currency, importance and keyword filters. While the current time is
within the defined pre/post news windows the strategy keeps the `IsNewsActive` flag set to `true` so that higher level logic can
avoid opening new positions or close existing orders.

Unlike the MQL source that downloaded and rendered the FxStreet calendar inside the chart, the StockSharp port uses the
platform built-in market data subscriptions:

* News are received via `Connector.SubscribeMarketData(Security, MarketDataTypes.News)`.
* Candle data (default 1 minute timeframe) is used to drive the time comparisons and to purge expired events.
* No manual WebRequest calls are necessary, therefore the strategy is fully self-contained inside the StockSharp ecosystem.

## Trading logic
1. The strategy subscribes to news and candles when it starts.
2. Every finished candle triggers a cleanup of expired events and a check whether the current time is inside any news window.
3. Incoming news are parsed and stored if they match:
   * Importance filter (`IncludeLow`, `IncludeMedium`, `IncludeHigh`).
   * Currency codes defined in `Currencies` (comma separated list).
   * Optional keyword defined in `SpecificNewsText` when `CheckSpecificNews` is enabled.
4. When a candle falls inside `StopBeforeNewsMinutes` before or `StartAfterNewsMinutes` after any stored event the strategy sets
   `IsNewsActive = true` and logs the "News time..." message. Once the window passes it reports "No news" and clears the flag.

The class itself does not place orders. It exposes enough state so that derived strategies or external orchestrators can stop or
resume trading around impactful economic releases.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `UseNewsFilter` | Enables or disables the blocking logic without changing the subscriptions. | `true` |
| `IncludeLow` | Accepts events that mention low impact keywords ("*" or "LOW"). | `false` |
| `IncludeMedium` | Accepts events that mention medium impact keywords ("**", "MEDIUM", "MODERATE"). | `true` |
| `IncludeHigh` | Accepts events that mention high impact keywords ("***" or "HIGH"). | `true` |
| `StopBeforeNewsMinutes` | Minutes before a matched event when trading must remain blocked. | `30` |
| `StartAfterNewsMinutes` | Minutes after the event when trading becomes allowed again. | `30` |
| `Currencies` | Comma separated currency tokens searched inside the news headline/story. | `USD,EUR,CAD,AUD,NZD,GBP` |
| `CheckSpecificNews` | When `true`, the keyword defined in `SpecificNewsText` must be present in the news text. | `false` |
| `SpecificNewsText` | Keyword (case insensitive) that filters the news stream when the previous flag is enabled. | `employment` |
| `CandleType` | Candle data type (time frame) that drives the time calculations. | `1 minute time frame` |

## State exposed to callers
* `IsNewsActive` – `true` when the current candle time is inside the restricted window.
* `_newsEvents` – internally stored list of upcoming news with their scheduled time and parsed importance.

## Implementation details
* All text processing is performed using uppercase comparisons to make the keyword checks case insensitive.
* Importance parsing supports both FxStreet style stars (`*`, `**`, `***`) and human readable words (`LOW`, `MEDIUM`, `HIGH`,
  `MODERATE`).
* Events are automatically sorted after insertion so that the earliest upcoming event is always processed first.
* Old events are removed once the current time is later than `StartAfterNewsMinutes` past the event time, keeping the in-memory
  list compact.
* The strategy un-subscribes from news when it stops to avoid leaving orphaned subscriptions on the connector.

## Usage tips
1. Combine the strategy with any execution logic by observing the `IsNewsActive` property and refraining from opening positions
   while it is `true`.
2. Extend the class or override `OnProcessMessage` if your data vendor uses custom fields to provide importance flags.
3. When testing in the Sandbox connector make sure that the selected security actually provides news events.

## Differences from the MQL version
* The chart annotations (labels and vertical lines) are not recreated because StockSharp visualisation depends on the host UI.
  All important information is delivered via the strategy log and properties instead.
* No explicit timezone correction is required; the connector feeds already use `DateTimeOffset` with precise offsets.
* The WebRequest based HTML parsing was replaced with platform native news subscriptions for reliability.
