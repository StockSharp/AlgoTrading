# 4026 – Pivots Strategy

## Overview

This strategy ports the MetaTrader 4 files located in `MQL/8550` (the **Pivots** indicator and the accompanying `Pivots_test` expert advisor) to StockSharp's high-level `Strategy` API. It keeps the original behaviour of calculating daily floor-pivot levels, staging a pair of opposing pending orders at the central pivot, and managing each resulting position with a fixed stop-loss, take-profit, and trailing stop.

## Pivot calculation

1. The strategy subscribes to a configurable *pivot timeframe* (`PivotCandleType`, daily by default).
2. Whenever a candle of that timeframe finishes, it derives classic floor-pivot levels from the previous day's OHLC prices:
   - `Pivot = (High + Low + Close) / 3`
   - `R1 = 2 × Pivot − Low`
   - `S1 = 2 × Pivot − High`
   - `R2 = Pivot + (High − Low)` and `S2 = Pivot − (High − Low)`
   - `R3 = 2 × Pivot + High − 2 × Low` and `S3 = 2 × Pivot − (2 × High − Low)`
3. The levels become active at the start of the next session. When this happens the strategy logs the values through `AddInfoLog` (for example: `Pivot levels for 2024-04-05: P=1.0924, R1=1.0956, …`).

## Pending order workflow

Once pivot levels are active, the strategy continuously ensures that two pending orders exist at the pivot price:

- **Buy Limit** @ `Pivot` with post-fill protection `SellStop` (stop-loss) at `S2` and `SellLimit` (take-profit) at `R2`.
- **Sell Stop** @ `Pivot` with post-fill protection `BuyStop` at `R2` and `BuyLimit` at `S2`.

All orders are submitted via the high-level helper methods `BuyLimit`, `SellStop`, `SellLimit`, and `BuyStop`. If an order fills, the code recalculates the average entry price for that direction, cancels existing protective orders, and sends a fresh stop/limit pair that covers the entire open volume (mirroring the MetaTrader behaviour where each position inherits the same S2/R2 protection). If the protective stop or take-profit executes, the related helpers are cleared automatically.

The strategy uses a single net position, so opposite fills will offset each other (unlike MetaTrader's ticket-based hedging). This is the only intentional deviation from the original expert.

## Trailing stop logic

- `TrailingStopPoints` defines the distance in indicator points (multiplied by the instrument `PriceStep`).
- For long positions the trailing stop activates once the price has moved more than that distance above the average entry. The protective `SellStop` is then moved closer to the market.
- For short positions the mirror logic applies, lowering the `BuyStop` as price moves favourably.
- Trailing updates are driven by the intraday series selected through `CandleType` (15-minute candles by default).

## Parameters

| Parameter | Description | Default |
| --- | --- | --- |
| `OrderVolume` | Volume of each pending order (lots/contracts). | `0.1` |
| `TrailingStopPoints` | Trailing stop distance in points. `0` disables the trailing logic. | `30` |
| `CandleType` | Intraday candle series used for trailing and for keeping the session schedule. | `15m` timeframe |
| `PivotCandleType` | Timeframe used to compute daily pivot levels. | `1D` timeframe |
| `LogPivotUpdates` | When `true`, pivot levels are written to the strategy log whenever they change. | `true` |

All numeric parameters are exposed through `StrategyParam<T>` so they can be optimised inside the StockSharp infrastructure.

## Logging and diagnostics

- Pivot updates are routed through `AddInfoLog`, which replaces the MetaTrader `Comment`/`ObjectSetText` output.
- Protective order management, position handling, and trailing logic rely solely on StockSharp's high-level helpers; no low-level order registration or indicator buffers are used.

## Usage notes

1. Attach the strategy to a connector that provides both daily and intraday candles for the chosen security.
2. Adjust the instrument's step if necessary (`PriceStep` is auto-detected; the fallback is `0.0001`).
3. Optionally tune `OrderVolume`, `TrailingStopPoints`, or the candle types to match the original MT4 setup.

No Python version is provided for this port as requested.
