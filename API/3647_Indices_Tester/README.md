# Indices Tester Strategy

## Overview
The **Indices Tester Strategy** is a direct port of the MetaTrader 5 expert advisor "Indices Tester". The system focuses on intraday index trading where a single long position is opened during a very narrow trading window. Trading decisions rely purely on time filters and operational limits:

- A single configurable candle stream drives the internal clock of the strategy.
- New positions can only be opened between the configured session start and end times.
- A fixed number of trades is allowed per day, preventing repeated re-entries.
- All open positions are forcibly closed at a defined liquidation time.
- The strategy operates on the long side only, mirroring the original expert advisor.

This implementation uses the high-level StockSharp API, subscribes to candle data with `SubscribeCandles`, and handles trading decisions in the `ProcessCandle` callback. No indicators are required, keeping the logic lean and focused on timing and risk controls.

## Trading Logic
1. **Daily reset** – the strategy keeps track of the current trading day. When a new day starts all counters are reset, allowing a fresh trade allowance for that day.
2. **Entry window** – only candles with a close time strictly inside the `[SessionStart, SessionEnd)` interval can trigger entries. This reproduces the `TimeStart` and `TimeEnd` checks from the original code.
3. **Position and trade limits** – entries are skipped if the number of trades already opened during the current day has reached `DailyTradeLimit`, or if the number of simultaneously open positions exceeds `MaxOpenPositions`.
4. **Order submission** – when all conditions align the strategy submits a market buy order for `TradeVolume` units. The counter of trades for the day is incremented immediately after order submission.
5. **Forced exit** – if a candle closes after `CloseTime` and there is an active long position, the strategy closes the position with a market sell order. This mirrors the `ClosePos()` timer logic from the MQL implementation.

The combination of the trade counter and position limiter guarantees that the system behaves as a simple single-trade-per-day scheduler by default while still allowing parameter tuning for more frequent activity.

## Parameters
| Name | Description |
| --- | --- |
| `CandleType` | Primary candle series driving the strategy clock (defaults to 1-minute candles). |
| `SessionStart` | Time of day when new trades are allowed to start. |
| `SessionEnd` | Time of day when new trades are no longer allowed. |
| `CloseTime` | Time of day when any remaining open position is liquidated. |
| `DailyTradeLimit` | Maximum number of entries allowed per day before trading is suspended. |
| `MaxOpenPositions` | Maximum number of simultaneously open long positions (counted in trade units). |
| `TradeVolume` | Market order volume used for each entry. |

## Notes and Differences
- StockSharp does not expose MetaTrader session tables, so the conversion relies on the exchange time from candle timestamps together with the `IsFormedAndOnlineAndAllowTrading()` guard.
- The original expert advisor used second-level timers; this port leverages candle closures to drive both entry timing and forced exits, which is sufficient for minute-level trading windows.
- Trade counts are reset at the beginning of each trading day detected from candle close times, keeping behaviour consistent across different time zones as long as the candle source matches the desired exchange.

## Usage Tips
- Ensure the configured `CandleType` matches the market being traded so that the time filters align with the desired session.
- Increase `DailyTradeLimit` if multiple attempts per day are required, for example when running on shorter time frames.
- Set `MaxOpenPositions` above `1` only when partial scaling into positions is desired; otherwise keep the default to mimic the MetaTrader script exactly.
