# Adaptive Grid MT4 (StockSharp Port)

## Overview

The strategy recreates the "Adaptive Grid Mt4" expert advisor for StockSharp's high level API. It drops a symmetric grid of
buy stop and sell stop orders around the current candle close. Grid distances are derived from the Average True Range (ATR) and
are therefore adaptive to market volatility. Each pending order expires after a configurable number of candles, keeping the
order book tidy in sideways markets.

When an entry order is filled the strategy immediately registers the matching take-profit and stop-loss orders at prices computed
from the ATR snapshot that produced the grid. Protective orders are one-to-one with the filled entry and persist until executed
or manually cancelled.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `GridLevels` | Number of stop orders above and below the market. Equivalent to the `nGrid` input of the EA. |
| `TimerBars` | Number of finished candles after which any pending entry is cancelled (MT4 `nBars`). |
| `PriceOffsetMultiplier` | ATR multiplier applied to the initial offset from the current price (`Poffset`). |
| `GridStepMultiplier` | ATR multiplier used for the spacing between consecutive grid levels (`Pstep`). |
| `StopLossMultiplier` | ATR multiplier defining the distance of the stop-loss attached to each order (`StopLoss`). |
| `TakeProfitMultiplier` | ATR multiplier defining the distance of the take-profit (`TakeProfit`). |
| `AtrPeriod` | ATR averaging period. Mirrors the hard-coded value of 14 from the script. |
| `OrderVolume` | Volume used for all pending orders (MT4 `Lot`). |
| `CandleType` | Time frame that drives grid recalculation (`Wtf`). |

## Trading Logic

1. Subscribe to candles of the configured `CandleType` and feed an ATR(14).
2. On each finished candle:
   - Advance the internal bar counter and cancel pending grid orders that exceeded `TimerBars`.
   - Skip further processing if the ATR is not formed, any grid order is still active, or the strategy already holds a position.
   - Compute the breakout offset, grid spacing, stop-loss and take-profit distances as `ATR * multiplier` values.
   - Place `GridLevels` pairs of buy stop and sell stop orders around the candle close, normalising prices with
     `Security.ShrinkPrice` to honour the instrument tick size.
3. When an entry fills, remove it from the tracked grid list and spawn the corresponding protective orders:
   - Long entries receive a `SellStop` stop-loss and a `SellLimit` take-profit.
   - Short entries receive a `BuyStop` stop-loss and a `BuyLimit` take-profit.
4. Protective orders are monitored via `OnOrderChanged` so that completed or cancelled entries are removed from the tracking
   lists.

## Notes

- The grid is only rebuilt when there are no open positions and all existing grid orders expired, matching the `What()` logic of
  the original EA.
- Prices are calculated from the candle close instead of the raw `Bid/Ask` tick. This keeps the implementation candle-driven
  while producing the same symmetric layout around the market.
- The ATR snapshot used for the grid is also used for protective orders to mimic MetaTrader's per-ticket stop and take-profit
  values.
- There is no Python translation yet, matching the request.
