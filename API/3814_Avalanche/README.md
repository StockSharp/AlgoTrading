# Avalanche Strategy

## Overview

The Avalanche strategy is a grid-style mean reversion system inspired by the original MetaTrader Avalanche v1.2 expert advisor. The idea is to monitor the relationship between price and a higher-timeframe equilibrium reference price (ERP) computed as a simple moving average. When price trades below the ERP the strategy expects a rebound toward the average and accumulates long positions. When price trades above the ERP the strategy looks for a decline and accumulates short positions. Each additional position is spaced by configurable distance thresholds, while every entry receives individual stop-loss and take-profit levels.

This StockSharp port focuses on the "toward" leg of the original algorithm. Away-from-ERP hedging orders from the MQL version are not replicated because StockSharp strategies operate on a single net position, but the grid stacking, buffering, and profit-taking logic remain faithful to the original approach.

## How it works

1. Subscribe to two candle series: the trading timeframe and an ERP timeframe that feeds the moving average.
2. Calculate an ERP simple moving average and determine whether price is positioned above or below it. A configurable buffer prevents frequent flips.
3. When a new ERP bias appears, close any open grid and wait for fresh signals.
4. Open an initial position in the direction that should bring price back toward the ERP (long below, short above) if the `OpenStartingOrders` flag is enabled.
5. Keep adding positions in the same direction when price advances by the `IntervalToward` distance (momentum stacking).
6. Add additional protective entries when price moves against the grid by `IntervalToward + StackBufferToward` (martingale stacking).
7. Each entry has its own stop-loss and take-profit target measured in points, ensuring that profitable legs can be closed individually while the grid continues to manage the remaining exposure.

## Parameters

| Name | Description |
| --- | --- |
| `BaseVolume` | Base order volume used before applying multipliers. |
| `TowardMultiplier` | Lot multiplier for standard toward-ERP entries. |
| `TowardInterestMultiplier` | Multiplier used when the instrument pays positive swap in the trading direction. |
| `IntervalToward` | Distance in points required to add a trend-following stack. |
| `StackBufferToward` | Additional buffer added to the interval when stacking against adverse price moves. |
| `TakeProfitToward` | Take-profit distance in points for each entry. Set to `0` to disable. |
| `StopLossToward` | Stop-loss distance in points for each entry. Set to `0` to disable. |
| `ErpPeriod` | Number of periods for the ERP simple moving average. |
| `ErpChangeBuffer` | Buffer (in points) applied around the ERP before switching bias. |
| `CandleType` | Trading timeframe used to trigger entries and exits. |
| `ErpCandleType` | Timeframe used to calculate the ERP moving average. |
| `OpenStartingOrders` | If enabled, immediately opens the first grid order when conditions are satisfied. |

## Differences vs. the original EA

- Only the toward-ERP leg is implemented because the StockSharp strategy maintains a single net position. Hedging away orders are omitted.
- Order execution relies on market orders instead of the pending stop orders used by the MQL version.
- Swap direction detection is preserved to choose between the standard and interest multipliers.

## Usage tips

- Adjust `IntervalToward` and `StackBufferToward` to control how aggressively the grid adds new trades.
- Ensure the selected instrument and timeframes provide enough liquidity; grid systems can accumulate sizeable exposure.
- Combine the strategy with external risk controls (equity stops, session filters) when running in production.
