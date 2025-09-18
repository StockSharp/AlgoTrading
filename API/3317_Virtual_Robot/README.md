# Virtual Robot Strategy

## Overview

The Virtual Robot strategy recreates the grid-based averaging approach of the original MetaTrader expert advisor. The algorithm maintains two independent virtual grids (long and short) on a configurable candle timeframe. Only when the number of virtual levels reaches the defined threshold are real market orders sent. This allows the strategy to simulate the MT4 behaviour where virtual levels guide actual position management.

## Trading Logic

1. **Virtual ladder creation** – On each finished candle the strategy compares the close to the open price.
   - If the candle closes higher than it opened, a new virtual long level is appended when the distance from the previous long level exceeds the pip step.
   - If the candle closes lower, the same logic is applied to the virtual short ladder.
   - The first `VirtualStepper` virtual trades use the base lot, later levels scale the size by `Multiplier`.
2. **Promotion to real orders** – After at least `StartingRealOrders` virtual levels exist for a side (or an existing basket draws down by at least one pip step), the strategy opens a real market order with volume calculated via the martingale multiplier (`Multiplier * distance / PipStep`).
3. **Basket management** – The strategy keeps track of:
   - The last execution price and volume for each side.
   - The weighted average of the open basket (real or virtual, depending on `RealAverageThreshold`).
4. **Take-profit logic** – Positions are closed when any of the following conditions is met:
   - Price moves by `MinTakeProfitPips` from the very first virtual order (single-level take-profit).
   - Price returns to the weighted virtual average plus/minus `AverageTakeProfitPips` for multi-level grids.
   - The calculated single-order or averaged take-profit level (derived from `TakeProfitPips` / `AverageTakeProfitPips`) is reached.
5. **Stop-loss logic** – A soft stop is derived from the last filled order using `StopLossPips`. When price crosses the protective level the basket is liquidated.
6. **Volume safety** – Lot sizes are normalised against the security metadata (`VolumeStep`, `MinVolume`, `MaxVolume`) and capped by `MaxVolume`.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `CandleType` | Candle series used to form the virtual ladder (default: 60-minute candles). |
| `StopLossPips` | Stop distance in pips from the latest filled order. |
| `TakeProfitPips` | Take-profit distance for single-order baskets. |
| `MinTakeProfitPips` | Minimum profit required to close a single virtual level. |
| `AverageTakeProfitPips` | Profit target applied to the weighted average of the basket. |
| `BaseVolume` | Base lot size for the first grid orders. |
| `MaxVolume` | Maximum allowed lot size. |
| `Multiplier` | Lot multiplier for averaged entries. |
| `RealStepper` | Number of filled real orders before the multiplier kicks in. |
| `VirtualStepper` | Virtual orders filled at the base lot before scaling. |
| `PipStepPips` | Minimum adverse excursion (in pips) between successive grid levels. |
| `MaxTrades` | Hard cap on the number of real orders per side. |
| `StartingRealOrders` | Number of virtual orders required before the first real order is placed. |
| `RealAverageThreshold` | Switches the averaged price from virtual to real once this many orders are filled. |
| `VisualMode` | Kept for parity with the MT4 input (no effect in StockSharp). |

## Implementation Notes

- The strategy uses net positions (StockSharp portfolio model) and therefore cannot maintain simultaneous independent long and short baskets like MT4 hedging mode. When both virtual ladders trigger, the most recent signal will flip the net position.
- Chart drawing from the original EA is intentionally omitted; all virtual levels are maintained internally.
- Price steps are derived from `Security.PriceStep` (with 10× adjustment for three/five-digit forex instruments) to mirror the MT4 pip conversion logic.
- Protective orders are modelled by monitoring prices in the candle handler and sending market exits rather than attaching broker-side stop/limit orders.

## Usage Tips

1. Ensure the instrument metadata (`PriceStep`, `VolumeStep`, `MinVolume`, `MaxVolume`) is filled so that pip conversion and lot normalisation match broker rules.
2. Start in simulation or on small volume to validate that grid distances and multipliers align with the broker you plan to trade.
3. Adjust `StartingRealOrders` and `RealStepper` to control the aggressiveness of the martingale scaling.
