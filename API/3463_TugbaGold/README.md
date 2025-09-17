# TugbaGold Strategy

## Overview

TugbaGold is a grid-based averaging expert advisor that originates from MetaTrader 5. The converted strategy recreates its martingale position sizing and basket management logic using StockSharp's high-level API. The system places new orders whenever the previous candle closes with directional momentum and progressively builds a grid of positions spaced by a configurable distance. Averaging exits are executed either by locking in profits on the extreme positions or by partially closing the basket depending on the selected mode.

## How it works

1. The strategy evaluates completed candles from the `CandleType` parameter. Signals use the *previous* candle, matching the original MT5 logic.
2. A bullish candle enables the placement of a new buy order. A bearish candle enables a new sell order.
3. Orders are added only if the distance from the best existing price in that direction exceeds `PointOrderStepPips`.
4. The first order uses `StartVolume`. Subsequent entries double the volume of the most favorable position while respecting `MaxVolume` and broker limits.
5. Once at least two positions exist, the strategy computes target prices that include the `MinimalProfitPips` buffer. The computation differs per exit mode:
   - **Average** – weighted average of the extreme positions plus the profit buffer.
   - **Partial** – combination of the worst and best tickets where the worst ticket uses `StartVolume` and the best uses its actual size.
6. When targets are reached the strategy closes the corresponding orders:
   - **Average mode** – closes both extreme entries entirely.
   - **Partial mode** – closes the worst entry completely and reduces the better entry by `StartVolume`.
7. Single standalone positions use `TakeProfitPips` to exit once price reaches the configured distance.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `TakeProfitPips` | Take-profit distance applied when only one position is open. Set to `0` to disable. |
| `StartVolume` | Initial volume for the first order in a grid sequence. |
| `MaxVolume` | Maximum order volume. `0` keeps the doubling sequence unbounded. |
| `CloseMode` | Exit mode: `Average` (close both extremes) or `Partial` (partial + full close). |
| `PointOrderStepPips` | Minimum distance in pips before a new averaging order can be added. |
| `MinimalProfitPips` | Additional profit buffer added to averaging targets. |
| `CandleType` | Candle series used for signal evaluation. |

## Position management

- Price steps are derived from `Security.PriceStep`. If it is not available a default of `0.0001` is used.
- Volumes are automatically normalized to the broker's minimum, maximum and step constraints.
- The strategy tracks filled positions internally and issues market orders (`BuyMarket` / `SellMarket`) when closing parts of the basket.
- Protection is enabled automatically through `StartProtection()` once the strategy starts.

## Notes and limitations

- The implementation assumes immediate fills for market orders, similar to the MT5 environment.
- Averaging signals rely on current best bid/ask quotes; ensure Level1 data is available for accurate execution.
- Because exits are driven by strategy logic, stop-loss levels from the original expert are not recreated.
- Use cautious risk management: martingale sizing can lead to large exposure if trends persist.

## Conversion details

- The averaging formulas and basket adjustments mirror the original source code.
- Position selection (best/worst tickets) is reproduced by tracking the highest and lowest open prices within each direction.
- All logic is executed inside the candle subscription using StockSharp's high-level API without resorting to low-level data access.
