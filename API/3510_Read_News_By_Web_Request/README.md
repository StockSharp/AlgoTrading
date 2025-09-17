# ReadNewsByWebRequest Strategy

## Overview
The **ReadNewsByWebRequestStrategy** reproduces the behaviour of the MetaTrader expert advisor “ReadNewsByWebRequest.mq4”.
It continuously downloads the weekly Forex Factory economic calendar and prepares a straddle around every upcoming
high-impact release. A pair of stop orders is submitted a configurable number of minutes before the scheduled publication
so that a sudden breakout in either direction is captured.

## External data
* **Source**: `https://nfs.faireconomy.media/ff_calendar_thisweek.xml` (Forex Factory XML calendar).
* **Update interval**: configurable (default 1 minute). The strategy issues a HTTP GET request and parses the XML feed in-place.
* **Filtering**: only events tagged with `High` impact and whose release time is still in the future are considered.
* **Time zone**: the feed provides GMT/UTC times. All comparisons are performed using the strategy server time
  (`Level1ChangeMessage.ServerTime`). Events marked as *All Day*, *Tentative*, *Holiday*, or without a precise time are ignored.

## Trading workflow
1. Start the strategy and connect it to a Forex symbol (the security’s price step and volume step are used to translate points into prices).
2. The strategy immediately downloads the calendar and schedules a timer that refreshes the data every `RefreshMinutes`.
3. Incoming level 1 updates are used to track the current best bid/ask.
4. When the clock reaches `LeadMinutes` before a news release, one buy-stop and one sell-stop order are placed around the market price.
5. Each pending order can inherit the built-in protection block (stop loss / take profit) configured via `StopLossPoints` and `TakeProfitPoints`.
6. Pending orders may be cancelled automatically after `PendingExpirationMinutes` or once the event reaches its release/expiry time.
7. After both orders become inactive the event is discarded from the internal queue.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `Volume` | `0.01` | Trade size expressed in lots. Rounded to the instrument volume step and constrained within min/max volume. |
| `StopLossPoints` | `300` | Distance from the fill price for the protective stop. Set to `0` to disable the stop loss. |
| `TakeProfitPoints` | `900` | Distance from the fill price for the profit target. Set to `0` to disable the take profit. |
| `BuyDistancePoints` | `200` | Offset (in points) added above the current ask price when placing the buy-stop order. |
| `SellDistancePoints` | `200` | Offset (in points) subtracted from the current bid price when placing the sell-stop order. |
| `LeadMinutes` | `5` | Minutes before the scheduled news when the stop orders are created. |
| `PendingExpirationMinutes` | `10` | Lifetime of the pending orders. `0` keeps orders until filled or manually cancelled. |
| `RefreshMinutes` | `1` | Interval between successive downloads of the Forex Factory calendar. |
| `ShowNewsLog` | `false` | When enabled the log prints the number of tracked high-impact events after every refresh. |

## Order management
* Orders are only sent when the strategy is online, trading is allowed, and the security exposes valid price/volume steps.
* The buy-stop is priced at `Ask + BuyDistancePoints * PriceStep` and the sell-stop at `Bid - SellDistancePoints * PriceStep`.
* If volume rounding results in a zero-sized order the placement is skipped and a warning is logged.
* Expired pending orders are cancelled through `CancelOrder`. Executed orders rely on `StartProtection` to issue stop-loss/take-profit exits.

## Risk controls
* `StartProtection` is enabled automatically whenever a stop loss or take profit distance is provided.
* Pending orders inherit the strategy risk settings; no martingale or averaging is performed.
* Only one pair of orders is created per news event. Once the release window passes the event is marked as completed.

## Notes and limitations
* Network access to Forex Factory is required. Connection failures are reported via warning log messages.
* The strategy ignores low and medium impact events to match the behaviour of the original expert advisor.
* Release times supplied without a specific minute cannot be traded automatically; those entries are skipped.
* Time conversions assume the feed uses UTC. Adjust the `LeadMinutes` parameter if the broker operates in a different time zone.
* Attach the strategy to instruments whose `PriceStep`, `Decimals`, `VolumeStep`, and min/max volume metadata are populated.

## Conversion details
* The original MetaTrader timer (60 seconds) has been mapped to the StockSharp strategy timer. The interval is user-configurable.
* XML parsing is implemented with `XDocument` instead of the manual string search used in the MQL program.
* Order validation (`CheckVolumeValue`) has been translated to `RoundVolume`, which honours the instrument’s volume constraints.
* Comments that were previously displayed on the MetaTrader chart are now optional log messages controlled by `ShowNewsLog`.
* Pending order expiration is replicated via the `PendingExpirationMinutes` parameter (default 10 minutes as in the MQL code).
