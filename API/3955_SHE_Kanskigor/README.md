# She Kanskigor Daily Strategy

## Overview
She Kanskigor Daily Strategy is a once-per-day breakout system that mirrors the original MetaTrader expert advisor `SHE_kanskigor.mq4`. The strategy evaluates the direction of the previous daily candle and opens a single market position inside a narrow time window at the start of the new trading day. It automatically monitors the position to close it by a configurable take-profit or stop-loss distance, expressed in security price steps.

## Trading Logic
1. Subscribe to both intraday candles (default: 1 minute) and daily candles for the selected security.
2. Update the stored daily open and close whenever a finished daily candle arrives.
3. On every finished intraday candle:
   - Reset the "traded today" flag when a new calendar date starts.
   - Manage the active position by checking whether the close price hits the stop-loss or take-profit thresholds.
   - Check whether the current time is inside the configured trading window (default start: 00:05, window length: 5 minutes).
   - If no position was opened yet today and a valid previous daily candle is available:
     - Go long when the previous daily open is higher than the close (bearish candle).
     - Go short when the previous daily open is lower than the close (bullish candle).
   - Skip trading when the previous day closed unchanged.
4. The strategy executes protective exits using market orders once the close price touches the configured thresholds.

## Parameters
| Name | Description | Default |
| ---- | ----------- | ------- |
| **Volume** | Order volume used for entries. | `0.1` |
| **Take Profit** | Profit target expressed in price steps. A value of `0` disables the target. | `35` |
| **Stop Loss** | Loss threshold expressed in price steps. A value of `0` disables the stop. | `55` |
| **Start Time** | Time of day (exchange time zone) when the entry window starts. | `00:05` |
| **Window (min)** | Duration, in minutes, of the entry window. | `5` |
| **Intraday Candle** | Candle data type used for intraday processing (default: 1-minute candles). | `TimeFrameCandleMessage(1m)` |

## Notes
- The strategy allows only one entry per trading day.
- Daily candle data must be available; otherwise the strategy waits until a completed candle arrives.
- Protective exits operate on the closing price of finished intraday candles.
- The code uses StockSharp high-level API (`SubscribeCandles` with `Bind`) and adheres to the project coding standards (tabs, English comments, and parameter metadata).
