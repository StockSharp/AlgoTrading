# Smoothing Average Crossover Strategy

## Overview
The Smoothing Average Crossover strategy replicates the logic of the original **Smoothing Average (barabashkakvn's edition)** MQL5 Expert Advisor. It combines a configurable moving average with a price distance filter measured in pips. When the market moves far enough away from the smoothed average, the strategy opens a position in the direction of the move (or the opposite side if reversal mode is enabled). Positions are closed once price reverts through an expanded channel around the moving average.

## Trading Logic
### Default mode (`ReverseSignals = false`)
- **Entry long:** the close price rises above the moving average minus `Entry Delta (pips)`.
- **Entry short:** the close price falls below the moving average plus `Entry Delta (pips)`.
- **Exit short:** the close price climbs above the moving average plus `Entry Delta (pips) × Close Delta Coefficient`.
- **Exit long:** the close price drops below the moving average minus `Entry Delta (pips) × Close Delta Coefficient`.

### Reverse mode (`ReverseSignals = true`)
- **Entry long:** the close price falls below the moving average plus `Entry Delta (pips)`.
- **Entry short:** the close price rises above the moving average minus `Entry Delta (pips)`.
- **Exit long:** the close price drops below the moving average minus `Entry Delta (pips) × Close Delta Coefficient`.
- **Exit short:** the close price climbs above the moving average plus `Entry Delta (pips) × Close Delta Coefficient`.

The moving average can be shifted forward by several candles. The strategy emulates this behaviour by keeping a small buffer of the most recent indicator values and using the value from `MaShift` bars ago. This matches the shifted line produced by the original MetaTrader implementation.

## Parameters
- `Candle Type` – data series used for calculations.
- `MA Length` – period of the smoothing average.
- `MA Shift` – number of bars that the moving average is shifted forward.
- `MA Type` – moving average method (simple, exponential, smoothed, or linear weighted).
- `Price Source` – candle price fed into the moving average (default: typical price).
- `Entry Delta (pips)` – distance from the moving average required to trigger entries. Converted to price using the instrument pip size.
- `Close Delta Coefficient` – multiplier applied to the entry delta when checking exit conditions.
- `Reverse Signals` – inverts long/short entry logic.
- `Trade Volume` – order size used for both long and short entries.

## Risk Management
- Orders are sent with the fixed `Trade Volume` parameter. The strategy does not scale in while a position is open.
- All exits are rule-based. No hard stop-loss or take-profit orders are submitted, but `StartProtection()` is invoked to enable the platform-level safety net.
- Reverse mode is available for counter-trend behaviour without altering other settings.

## Implementation Notes
- The pip size is derived from `Security.PriceStep`. Three- or five-digit FX symbols receive the same 10× adjustment as in the MQL5 code.
- The moving average uses the `Price Source` selection so that typical, median, or other candle prices can be matched to the original EA settings.
- Entry and exit comparisons use the candle close as a stable proxy for bid/ask checks in the source Expert Advisor.
- All comments inside the C# code are provided in English, as required by the conversion guidelines.
