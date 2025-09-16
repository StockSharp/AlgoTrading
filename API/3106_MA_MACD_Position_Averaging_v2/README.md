# MA MACD Position Averaging v2 Strategy

## Overview
The **MA MACD Position Averaging v2 Strategy** is a direct translation of Vladimir Karputov's MetaTrader expert advisor. It combines a weighted moving average filter, a MACD confirmation block, and an averaging module that increases exposure when existing trades move against the position. The StockSharp version keeps the original signal hierarchy, processes indicators on finished candles, and manages protective logic (stop loss, take profit, trailing) in code to reproduce broker-side behaviour from MQL.

## Trading Logic
1. **Indicator preparation**
   - A configurable moving average calculates on the selected candle type and price component. The `MaShift` parameter emulates MetaTrader's forward shift by reading values from older candles, while `BarOffset` lets you evaluate the current or a previous bar.
   - A MACD signal indicator produces the main and signal lines using customisable fast, slow, and signal periods and an applied price matching the original expert advisor.
2. **Signal validation**
   - Long setups require both MACD lines to be negative, the price to sit above the shifted moving average, and the price distance to the average to exceed `MaIndentPips` (converted to absolute price using the instrument pip size).
   - Short setups mirror the conditions: both MACD lines must be positive, price must stay below the shifted moving average, and the gap to the average must be at least `MaIndentPips`.
   - The ratio filter `MacdRatio` enforces `MACD_main / MACD_signal >= MacdRatio` (using absolute decimal division) before a trade is allowed.
   - When `ReverseSignals = true` the direction of the market order is inverted after all conditions pass.
3. **Position lifecycle**
   - If **no position** exists, the strategy opens a market order with the configured `OrderVolume` (rounded by the security volume step) in the computed direction. Stop-loss and take-profit levels are applied immediately according to `StopLossPips` and `TakeProfitPips`.
   - If **an exposure already exists**, the strategy never opens the opposite side. Instead it either:
     - Closes everything if longs and shorts are detected simultaneously (safety net mirroring the MQL check), or
     - Invokes the averaging block for the current side.
4. **Averaging module**
   - For longs the algorithm finds the lowest-priced open leg whose unrealised loss exceeds `StepLossPips`. For shorts it selects the highest-priced losing leg.
   - Once a candidate is found a new market order is sent with volume `CandidateVolume × LotCoefficient` (after adjusting to the allowed volume step/min/max). This reproduces the geometric progression from the original expert.
   - New legs inherit the same stop-loss and take-profit distances and become eligible for trailing updates.
5. **Risk controls**
   - A trailing stop activates only when both `TrailingStopPips` and `TrailingStepPips` are greater than zero. For longs the stop moves to `Close - TrailingStopPips` once profit exceeds `TrailingStopPips + TrailingStepPips`; shorts behave symmetrically.
   - Manual stop-loss and take-profit checks are performed on each finished candle. When triggered, a market order closes the exact leg and removes it from the averaging list.

## Parameters
| Parameter | Description |
| --- | --- |
| **OrderVolume** | Base volume for the very first trade in a cycle. |
| **StopLossPips** | Stop-loss distance in pips. Set to zero to disable the stop. |
| **TakeProfitPips** | Take-profit distance in pips. Set to zero to disable the target. |
| **TrailingStopPips** | Distance between price and the trailing stop. Works together with `TrailingStepPips`. |
| **TrailingStepPips** | Additional favourable move required before the trailing stop is updated. |
| **StepLossPips** | Minimal loss (in pips) required before an averaging leg is added. |
| **LotCoefficient** | Multiplier applied to the selected losing leg volume when averaging. |
| **BarOffset** | Number of bars back to read indicator values (0 = current finished bar). |
| **ReverseSignals** | Inverts long/short execution while keeping the same filters. |
| **MaPeriod** | Moving average period. |
| **MaShift** | Forward shift applied to the moving average (MetaTrader style). |
| **MaMethod** | Moving average smoothing method (Simple, Exponential, Smoothed, Weighted). |
| **MaPrice** | Candle price component used for the moving average. |
| **MaIndentPips** | Minimal price distance from the moving average before entering. |
| **MacdFastPeriod** | Fast EMA period for MACD. |
| **MacdSlowPeriod** | Slow EMA period for MACD. |
| **MacdSignalPeriod** | Signal EMA period for MACD. |
| **MacdPrice** | Applied price used in the MACD calculation. |
| **MacdRatio** | Minimal ratio between MACD main and signal lines. |
| **CandleType** | Candle series used for all calculations. |

## Implementation Notes
- Pip size is calculated from the security's price step, reproducing the 3/5-digit adjustment from the MQL version. This keeps pip-based distances identical across Forex symbols.
- All indicator buffers use queues to emulate MetaTrader's `ma_shift` and `bar` indexing without calling historical lookup methods prohibited by the project rules.
- Volume adjustments respect `Security.VolumeStep`, `Security.MinVolume`, and `Security.MaxVolume`, preventing invalid order sizes when LotCoefficient multiplies the exposure.
- Protective logic (stops, takes, trailing) runs entirely in the strategy layer, so there is no dependency on broker-side position modification APIs.
- The class resides in the `StockSharp.Samples.Strategies` namespace and follows the repository requirement of using tab indentation and English comments exclusively.
