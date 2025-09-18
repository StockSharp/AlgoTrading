# Volume Trader V2 Strategy

## Overview
Volume Trader V2 is a direct conversion of the MetaTrader expert advisor `Volume_trader_v2_www_forex-instruments_info.mq4`. The original system observes how the total volume of the latest candles evolves and uses this short-term flow to decide whether a simple long or short exposure should be active. The StockSharp port keeps the one-position-at-a-time behaviour, the time-of-day filter and the requirement to act only once per completed candle.

The strategy subscribes to a configurable candle series and caches the volume of the last two finished candles. When a new bar closes, the volumes from the previous two bars (MetaTrader's `Volume[1]` and `Volume[2]`) are compared and an updated trade direction is produced:

- `Volume[1] < Volume[2]` generates a **long** bias.
- `Volume[1] > Volume[2]` generates a **short** bias.
- Equal volumes or disabled trading hours remove any open exposure.

Before sending a new order the current position is flattened if it points in the opposite direction so that the StockSharp implementation matches the MetaTrader order lifecycle.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `CandleType` | 5-minute time frame | Data type requested from `SubscribeCandles`. Set it to match the chart period used in MetaTrader. |
| `StartHour` | 8 | First trading hour (inclusive). Signals outside the window are ignored and any position is closed. |
| `EndHour` | 20 | Last trading hour (inclusive). When the current candle starts after this hour the strategy stays flat. |
| `TradeVolume` | 0.1 | Lot size replicated from the EA. The value is also assigned to `Strategy.Volume` so helper methods use the same amount. |

All parameters are regular `StrategyParam<T>` instances so they can be optimised or exposed through the UI.

## Trading Logic
1. Handle only finished candles to guarantee bar-by-bar parity with the EA.
2. Cache `Volume[1]` and `Volume[2]` equivalents in `_previousVolume` and `_twoBarsAgoVolume` before any signal evaluation.
3. Validate that the candle start time falls between `StartHour` and `EndHour` (inclusive). Outside this range any active position is closed and no new orders are created.
4. Compute the desired direction:
   - Long when the most recent volume is lower than the previous bar.
   - Short when the most recent volume is higher than the previous bar.
   - Neutral otherwise.
5. If the desired direction differs from the current position, close the opposite position first (`BuyMarket(-Position)` or `SellMarket(Position)`).
6. Enter the new position using the configured `TradeVolume` only when the strategy is flat or positioned in the opposite direction.
7. Update the cached volumes so the next cycle still compares the last two completed candles.

This flow guarantees that no orders are placed while a candle is still building and that the StockSharp strategy reacts exactly once per bar, just like the MetaTrader implementation that relied on `LastBarChecked`.

## Additional Notes
- `StartProtection()` is called in `OnStarted` to reuse the framework protection helper that keeps track of the current position.
- The `Comment` property mirrors the EA diagnostic messages (`"Up trend"`, `"Down trend"`, `"No trend..."` or `"Trading paused"`) to simplify monitoring.
- The strategy does not maintain extra collections, and it leverages the high-level candle subscription API in line with the project guidelines.
- Set the candle type, security and volume to match the instrument and timeframe originally used in MetaTrader for comparable results.
