# Hans123 Trader v2 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Hans123 Trader v2 is a breakout strategy that places pending stop orders around the recent trading range. It mirrors the MetaTrader implementation by Vladimir Karputov and is adapted to the StockSharp high-level API. The system focuses on capturing momentum when price escapes the most recent 80-bar range while managing protective exits and a trailing stop.

## Core Idea

- Monitor a configurable candle series (default 1-hour bars).
- During the active session window, compute the highest high and lowest low over the last *N* candles (default 80).
- Place a buy stop order at the highest high and a sell stop order at the lowest low when the market is far enough from the current bid/ask.
- Limit the total number of working pending orders to avoid over-exposure.
- Once a position is opened, cancel the remaining pending orders, apply stop-loss and take-profit offsets (measured in pips), and activate a trailing stop.

## Trade Management

- **Entries**: Stop orders are placed only while the time of the processed candle falls between the configured start and end hours. Orders are ignored outside that window.
- **Position Protection**: When a new position is created, the strategy immediately registers protective stop-loss and take-profit orders using the configured pip distances.
- **Trailing Stop**: If enabled, the stop-loss order is re-issued closer to price once it moves in the position's favour by more than the trailing threshold plus step.
- **Order Cleanup**: Exiting a position cancels the protective orders, and any fresh entry cancels the opposite pending orders, matching the behaviour of the original MQL logic.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `Volume` | Order size used when submitting breakout and protective orders. |
| `StopLossPips` | Distance in pips between the entry price and the protective stop-loss. Set to `0` to disable. |
| `TakeProfitPips` | Distance in pips between the entry price and the take-profit order. Set to `0` to disable. |
| `TrailingStopPips` | Initial trailing stop distance in pips. `0` disables trailing. |
| `TrailingStepPips` | Minimum additional profit in pips required before moving the trailing stop again. Must be non-zero when trailing is enabled. |
| `StartHour` | Session opening hour (inclusive) for placing new pending orders. |
| `EndHour` | Session closing hour (exclusive) for placing new pending orders. Must be greater than `StartHour`. |
| `MaxPendingOrders` | Maximum number of simultaneous breakout orders (buy + sell) allowed. |
| `BreakoutPeriod` | Lookback length (in candles) for the highest high and lowest low calculations. |
| `CandleType` | Candle series processed by the strategy (timeframe or other candle data type). |

## Notes

- Pip size is derived from the security's price step. For 3- and 5-digit forex symbols, the point value is adjusted to match the MQL definition of a pip.
- The strategy relies on the `Security.BestBid`/`BestAsk` snapshots when available. If depth data is not present, it falls back to the current candle close price to evaluate the minimum distance from the market.
- Protective orders are re-created whenever they need to be moved, mirroring the `PositionModify` logic from the original expert advisor.
- The implementation keeps the logic purely in C# with no Python translation, as requested.
