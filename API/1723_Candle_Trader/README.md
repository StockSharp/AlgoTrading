# Candle Trader Strategy

## Overview

The **Candle Trader Strategy** analyses the direction (bullish or bearish) of the last four completed candles to identify short‑term reversal opportunities. It operates on a single instrument and sends market orders with predefined take‑profit and stop‑loss levels.

## Strategy Logic

1. **Long entry (direct)** – last candle bullish, previous two bearish.
2. **Long entry (continuation)** – last candle bullish, previous candle bearish, the two candles before that bullish. This rule is enabled only when *Continuation* is `true`.
3. **Short entry (direct)** – last candle bearish, previous two bullish.
4. **Short entry (continuation)** – last candle bearish, previous candle bullish, the two candles before that bearish. Enabled only when *Continuation* is `true`.
5. If *Reverse Close* is enabled and a new signal appears opposite to the current position, the strategy closes the existing position before opening a new one.
6. All orders are protected by fixed take‑profit and stop‑loss values measured in price steps.

## Parameters

| Name | Description |
|------|-------------|
| `Volume` | Order volume for each trade. |
| `TakeProfitTicks` | Take‑profit distance in price steps. |
| `StopLossTicks` | Stop‑loss distance in price steps. |
| `Continuation` | Enables the continuation patterns for additional entries. |
| `ReverseClose` | Closes an open position before entering the opposite direction. |
| `CandleType` | Candle timeframe used for analysis. |

## Notes

- The strategy evaluates only finished candles.
- It uses market orders and cancels any active orders before sending new ones.
- The stop‑loss and take‑profit levels are applied via `StartProtection`.
- Position size can be optimized through the `Volume` parameter.
