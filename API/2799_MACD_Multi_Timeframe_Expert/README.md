# MACD Multi-Timeframe Expert Strategy

## Overview
This strategy replicates the original "MACD Expert" MetaTrader robot inside the StockSharp framework. It synchronizes MACD trends across four timeframes—5 minutes, 15 minutes, 1 hour, and 4 hours—and only allows a new position when every timeframe points in the same direction. The goal is to capture multi-timeframe momentum alignment while filtering out periods of high spread.

## Data & Indicators
- **Candles**: 5m (execution), 15m, 1h and 4h confirmations. All candles use close prices and finished bars only.
- **Indicator**: `MovingAverageConvergenceDivergenceSignal` with defaults 12/26/9. Each timeframe has its own MACD instance so that signals do not interfere.
- **Level 1 Quotes**: Best bid/ask quotes are consumed to monitor the live spread before opening trades.

## Trading Logic
1. Wait for all four MACD instances to emit a completed value.
2. Compute the relationship between the MACD line and signal line on every timeframe.
3. Enforce a maximum spread filter measured in price points (price steps).
4. Open at most one position at a time; existing positions must finish via stop-loss or take-profit before a new order is allowed.

### Long Setup
- MACD signal line is above the MACD line on **all** monitored timeframes.
- Spread does not exceed `MaxSpreadPoints`.
- A long position is opened with `OrderVolume` lots at the close of the latest 5-minute candle.

### Short Setup
- MACD signal line is below the MACD line on **all** monitored timeframes.
- Spread does not exceed `MaxSpreadPoints`.
- A short position is opened with `OrderVolume` lots at the close of the latest 5-minute candle.

### Position Management
- Long trades place logical targets at `TakeProfitPoints` above the entry and stops `StopLossPoints` below it.
- Short trades place logical targets at `TakeProfitPoints` below the entry and stops `StopLossPoints` above it.
- Exits trigger when the intrabar high/low of a finished 5-minute candle touches the respective target or stop level.
- While in position the strategy ignores opposite signals; it waits until the trade is closed by stop or take-profit before reacting again, matching the original MQL logic.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `OrderVolume` | 0.1 | Position size in lots (mirrors the `Lots` input of the MQL version). |
| `StopLossPoints` | 200 | Distance to the protective stop in price points. |
| `TakeProfitPoints` | 400 | Distance to the profit target in price points. |
| `MaxSpreadPoints` | 20 | Maximum allowed spread in price points before entries are skipped. |
| `FastPeriod` | 12 | Fast EMA length inside each MACD instance. |
| `SlowPeriod` | 26 | Slow EMA length inside each MACD instance. |
| `SignalPeriod` | 9 | Signal EMA length inside each MACD instance. |
| `FiveMinuteCandleType` | 5-minute candles | Primary execution timeframe. |
| `FifteenMinuteCandleType` | 15-minute candles | First confirmation timeframe. |
| `HourCandleType` | 1-hour candles | Second confirmation timeframe. |
| `FourHourCandleType` | 4-hour candles | Third confirmation timeframe. |

## Implementation Notes
- Uses `BindEx` to read strongly typed MACD values without calling `GetValue`, following the project guidelines.
- A shared helper converts the MACD/signal relationship into `{-1, 0, 1}` flags to simplify confirmation checks.
- Spread validation divides the best ask minus best bid by `Security.PriceStep` so the threshold matches MetaTrader "points" behavior.
- Trade events are logged with `LogInfo` to aid debugging when testing in Designer or Runner.
- No Python translation is provided, per the task requirements; only the C# version is included.
