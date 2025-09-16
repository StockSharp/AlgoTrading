# VLT Trader Strategy

## Overview

The **VLT Trader Strategy** is a volatility contraction breakout system converted from the original MQL implementation. It monitors
recent candle ranges and prepares stop orders whenever the most recent completed candle becomes the smallest range within a
configurable historical window. The goal is to capture explosive moves after a tight consolidation period.

## Trading Logic

1. **New bar processing** – the strategy evaluates conditions only once per new candle. The current candle must open below the
   previous candle high to avoid trading gaps that jump through the breakout level.
2. **Volatility filter** – the range of the most recent finished candle is compared with the smallest range among the last
   `CandleCount` finished candles whose range is below `MaxCandleSizePips`. If the most recent candle is strictly smaller, the
   setup is valid.
3. **Entry placement** – when the setup is valid, two stop orders are prepared:
   - A **buy stop** `10` pips above the previous high when the net position is not long.
   - A **sell stop** `10` pips below the previous low when the net position is not short.
   Existing pending orders of the same type are cancelled before registering new ones.
4. **Risk management** – once a stop order triggers and opens a position, protective orders are attached automatically:
   - Take-profit at `TakeProfitPips` above/below the entry price.
   - Stop-loss at `StopLossPips` below/above the entry price.
   Protective orders are cancelled when the position returns to zero.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `Volume` | Order volume sent with every stop order. |
| `TakeProfitPips` | Distance in pips used for the take-profit order after entry. |
| `StopLossPips` | Distance in pips used for the protective stop after entry. |
| `MaxCandleSizePips` | Upper bound for the historical candle ranges considered in the volatility filter. |
| `CandleCount` | Number of historical candles used to find the minimum acceptable range. |
| `CandleType` | Candle time frame used for the analysis. |

## Implementation Notes

- Pip size is derived from the security price step. When the step is below or equal to `0.001`, it is multiplied by `10` to
  emulate the MetaTrader pip definition for 3- or 5-decimal instruments.
- Candle ranges are stored in a FIFO queue limited to `CandleCount` elements, matching the historical scanning performed in the
  original Expert Advisor.
- All orders are created through the high-level StockSharp API (no manual order registration) and are automatically cancelled when
  outdated or when the position closes.
- Comments inside the code are written in English, while README files provide detailed multilingual documentation.
