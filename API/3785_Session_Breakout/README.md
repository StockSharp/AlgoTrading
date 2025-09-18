# Session Breakout Strategy

## Overview
The Session Breakout strategy replicates the MetaTrader expert advisor "Session breakout". It watches the European morning sessi
on and measures its price range. When that range is sufficiently tight, the strategy prepares to trade breakouts during the U.S.
afternoon session using StockSharp's high-level API. The implementation enforces at most one long and one short entry per day a
nd automatically attaches protective orders (stop loss and take profit) to every position.

## Trading logic
- Reset the state at the beginning of every trading day and skip weekends. Mondays are optional and controlled by a parameter.
- Track finished candles during the European session (default 06:00–12:00) and record the highest high and lowest low.
- At the start of the U.S. session the captured range is classified as "small" when its width is less than `SmallSessionThreshol
dPips`.
- If the range is small, monitor U.S. session candles (default 12:00–16:00) and wait until at least one U.S. bar has closed (`Eu
ropeSessionStartHour + 5` to `EuropeSessionStartHour + 10`).
- A long breakout is triggered when the entire candle stays above the European high plus a configurable buffer (`BreakoutBuffer
Pips`). A short breakout requires the candle to stay below the European low minus the buffer.
- After entering a position, the strategy attaches stop-loss and take-profit levels expressed in pips and prevents additional en
tries in the same direction for the rest of the day.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `Volume` | Order volume used for both long and short breakouts. |
| `EuropeSessionStartHour` | Hour when the European range tracking begins. |
| `EuropeSessionEndHour` | Hour when the European range tracking stops. |
| `UsSessionStartHour` | Hour that marks the beginning of the U.S. session window. |
| `UsSessionEndHour` | Hour that marks the end of the U.S. session window. |
| `SmallSessionThresholdPips` | Maximum width (in pips) for the European range to qualify as a squeeze. |
| `BreakoutBufferPips` | Extra buffer added above/below the range before triggering breakouts. |
| `TradeOnMonday` | Enables trading on Mondays. Weekends are always skipped. |
| `TakeProfitPips` | Distance between the entry price and the take-profit level. |
| `StopLossPips` | Distance between the entry price and the stop-loss level. |
| `CandleType` | Candle series used for all calculations (15-minute candles by default). |

## Notes
- The pip size is derived from the instrument `PriceStep`. Adjust the pip-based parameters to match the contract specification
s of the selected security.
- Because orders are generated when a qualifying candle closes, fills happen at the close price of that candle in backtests. Liv
e fills may vary depending on market conditions.
- Only one long and one short trade can be opened per day. The logic mirrors the original expert advisor behaviour while using S
tockSharp's position-based risk management helpers.
