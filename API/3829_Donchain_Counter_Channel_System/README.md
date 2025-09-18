# Donchain Counter-Channel System

## Overview
The **Donchain Counter-Channel System** reproduces the 2005 MetaTrader 4 expert advisor by Michal Rutka. It watches for turns in a 20-day Donchian Channel calculated on daily candles. When the lower band turns upward the strategy assumes that sellers failed to push price to new lows and buys the next session at market. When the upper band turns downward the strategy interprets that as a momentum loss on rallies and sells short at market. Protective stops are always aligned with the opposite Donchian band so that exits mirror the original stop-management logic.

Only one entry is allowed every 24 hours, matching the rule from the article that restricts the system to at most one order per day. This implementation uses StockSharp's high-level API with indicator bindings so the Donchian values arrive together with each completed candle.

## Trading Logic
1. Subscribe to the configured `CandleType` (daily by default) and evaluate a `DonchianChannels` indicator with the selected `ChannelPeriod`.
2. Whenever a candle finishes:
   - If a long position is open, move the stop level to the current lower band when it rises and exit if the candle low touches that level.
   - If a short position is open, move the stop level to the current upper band when it falls and exit if the candle high touches that level.
   - If there is no position, skip entries when the last trade happened less than `TradeCooldown` ago.
   - Go long when the lower Donchian band on the previous candle is higher than on the candle before it, signalling an upswing in the channel floor. Set the initial stop to the current lower band.
   - Go short when the upper Donchian band on the previous candle is lower than on the candle before it, signalling a downswing in the channel ceiling. Set the initial stop to the current upper band.
3. Continue trailing the stop along the bands until price reverses through them, which closes the position.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `Volume` | `1` | Order size for both long and short entries. |
| `ChannelPeriod` | `20` | Number of candles used to compute the Donchian upper and lower bands. |
| `TradeCooldown` | `1 day` | Minimum waiting period before a new entry is permitted. |
| `CandleType` | `Daily` | Candle series on which the Donchian Channel is calculated. |

## Indicators and Data
- **Donchian Channels** – provides the upper and lower channel boundaries used for trend-turn detection and for trailing stops.
- **Daily Candles (default)** – supply closing times required for the 24-hour cooldown and for evaluating indicator turns.

## Implementation Notes
- The strategy uses `BindEx` to receive a typed `DonchianChannelsValue` in the candle handler, ensuring both bands are available simultaneously.
- Stops are simulated by monitoring candle highs and lows against the stored band value, just like the original EA updated its stop-loss on every new bar.
- The cooldown timer is updated only on new entries, mirroring the source script that prevented multiple entries within the same trading day.
