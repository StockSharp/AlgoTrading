# Money Rain Recovery Strategy

## Overview
- Conversion of the MetaTrader 4 expert advisor **MoneyRain.mq4** to the StockSharp high-level API.
- Trades on the close of finished candles using a DeMarker oscillator filter.
- Keeps the original fixed stop-loss / take-profit exits and the volume-recovery block that increases the next order size after a loss sequence.

## Trading Logic
1. Subscribe to the configured `CandleType` (default: 1-hour candles) and compute DeMarker with period `DeMarkerPeriod`.
2. When no position is active and no order is pending:
   - Buy if the current DeMarker value is above `Threshold`.
   - Sell otherwise.
   - The order size is either the base volume or the recovery volume calculated from previous losses.
3. While a position is open the strategy watches each completed candle:
   - Longs close when the candle low touches the stop level (`StopLossPoints` below the entry) or the candle high reaches the target (`TakeProfitPoints` above the entry).
   - Shorts mirror the same rules with inverted levels.
4. After every exit the money-management block updates the consecutive loss counters and prepares the next order size. When the losing streak reaches `LossesLimit` the strategy stops opening new positions and logs a warning.

## Money Management
- `BaseVolume` is normalized to the exchange rules (`Security.VolumeStep`, `Security.MinVolume`, `Security.MaxVolume`). If the normalized size drops below the minimum lot, the entry is skipped.
- After each losing trade the strategy stores the volume used (scaled by the base lot) and resets the consecutive-profit counter. The very next profitable trade uses the original MoneyRain formula `baseLot × lossesVolume × (StopLoss + spread) / (TakeProfit − spread)` to recover losses. Subsequent wins revert to the base volume, and the loss accumulator is cleared after two or more consecutive profits.
- If `FastOptimization` is enabled the recovery block is bypassed and every entry uses the normalized base volume.
- Spread for the recovery formula is estimated from the latest level-1 best bid/ask. If quotes are unavailable the spread falls back to zero.

## Parameters
| Parameter | Description | Default | Notes |
|-----------|-------------|---------|-------|
| `DeMarkerPeriod` | Length of the DeMarker oscillator. | `10` | Must be greater than zero. |
| `TakeProfitPoints` | Distance to the take-profit in price steps. | `50` | Converted by multiplying with `Security.PriceStep`. |
| `StopLossPoints` | Distance to the stop-loss in price steps. | `50` | Must stay positive so the recovery formula remains valid. |
| `BaseVolume` | Baseline order volume. | `1` | Normalized to instrument limits before submission. |
| `LossesLimit` | Maximum consecutive losing trades allowed. | `1 000 000` | When reached, entries are paused until the strategy is reset. |
| `FastOptimization` | Disable recovery sizing during optimizer runs. | `true` | Keeps the model lightweight for bulk tests. |
| `Threshold` | DeMarker threshold separating buy and sell signals. | `0.5` | Matching the MT4 constant from the source code. |
| `CandleType` | Candle data series used for signals. | `1h` | Change for other timeframes or custom aggregations. |

## Usage Notes
- Set correct `Security.PriceStep`, `Security.VolumeStep`, `Security.MinVolume` and `Security.MaxVolume` values so price/volume conversions remain valid.
- Positive `StopLossPoints` and `TakeProfitPoints` are required. Leaving them at zero prevents exits, diverging from the original EA.
- The strategy waits for actual fills before updating its internal state, so it handles partial fills by tracking the weighted exit price.
- When the loss limit triggers, the next profitable trade is not taken—restart or reset the strategy to resume trading.
