# XCCI Histogram Vol Direct Strategy

## Overview
The **XCCI Histogram Vol Direct Strategy** is a conversion of the MQL5 expert `Exp_XCCI_Histogram_Vol_Direct`. The system multiplies the Commodity Channel Index (CCI) by volume, smooths both series with a configurable moving average, and then evaluates the slope of the smoothed oscillator. When the directional coloring of the histogram flips, the strategy closes positions against the move and opens new trades in the emerging direction. The logic works on finished candles only and therefore behaves deterministically on historical and live data.

The original expert advisor used a proprietary smoothing library with multiple algorithms, volume-based threshold bands, and time-shifted order execution. The StockSharp port retains the configurable inputs, approximates the smoothing choices with available indicators, and implements the same open/close sequencing using the high-level API.

## Market Regime & Edge
- Designed for markets where volume expansion accompanies momentum bursts.
- Prefers timeframes with clear swings (default: 2-hour candles) but can be tuned from intraday to swing horizons.
- Signals react to a change in the slope of smoothed CCI*volume; therefore it behaves like a momentum reversal detector.

## Indicators & Processing Pipeline
1. **Commodity Channel Index (CCI)** – computed on the selected candle type with period `CciPeriod`.
2. **Volume Source** – either `Tick` or `Real` (both mapped to candle volume because tick counts are not available in StockSharp candles).
3. **Weighted Oscillator** – multiply CCI by the chosen volume stream.
4. **Smoothing** – apply the selected moving-average family to both the weighted oscillator and raw volume using length `SmoothingLength`.
   - `Sma` → SimpleMovingAverage
   - `Ema` → ExponentialMovingAverage
   - `Smma` → SmoothedMovingAverage
   - `Lwma` → WeightedMovingAverage
   - `Jjma` → JurikMovingAverage
   - `Jurx` → ZeroLagExponentialMovingAverage
   - `Parabolic` → ArnaudLegouxMovingAverage (phase parameter mapped to ALMA offset)
   - `T3` → TripleExponentialMovingAverage
   - `Vidya` → ExponentialMovingAverage (best available approximation)
   - `Ama` → KaufmanAdaptiveMovingAverage
5. **Directional Color** – compare the latest smoothed oscillator value with the previous one. Rising values are colored `0` (bullish), falling values `1` (bearish), and equal values inherit the prior color just like the original indicator buffer.
6. **Signal Memory** – store the recent colors so the strategy can inspect the bar specified by `SignalBar` and the bar before it.

## Trading Rules
### Long Management
- **Entry**: If the color of the signal bar is `1` (bearish) but the bar before it was `0` (bullish), open a long position provided `AllowLongEntries = true` and the current net position is not already long. The order size equals `Volume + |Position|` so any short exposure is flattened first.
- **Exit**: Whenever the bar before the signal bar is bullish (`0`) and `AllowShortExits = true`, close any open short position to avoid fighting the new upswing.

### Short Management
- **Entry**: If the signal bar color becomes `0` after a prior `1`, open a short position when `AllowShortEntries = true` and the account is not already net short. The order size mirrors the long logic.
- **Exit**: When the bar before the signal bar is bearish (`1`) and `AllowLongExits = true`, close long exposure.

### Risk Controls
- `StopLossPoints` and `TakeProfitPoints` translate into price-point offsets using the instrument’s `PriceStep` and are applied through `StartProtection`.
- Protective orders are activated for every trade; set both values to `0` to disable an individual leg.

## Parameter Reference
| Parameter | Description | Default |
|-----------|-------------|---------|
| `CciPeriod` | Length of the Commodity Channel Index. | `14` |
| `Smoothing` | Moving-average family used for smoothing both oscillator and volume. | `T3` |
| `SmoothingLength` | Period of the smoothing filters. | `12` |
| `SmoothingPhase` | Phase/offset value mapped to ALMA offset; kept for compatibility. | `15` |
| `HighLevel2`, `HighLevel1`, `LowLevel1`, `LowLevel2` | Threshold multipliers preserved from the indicator (useful for diagnostics/visualization). | `100`, `80`, `-80`, `-100` |
| `SignalBar` | Look-back index of the bar that defines the signal (0 = latest closed candle). | `1` |
| `AllowLongEntries` / `AllowShortEntries` | Enable or disable opening trades in a direction. | `true` |
| `AllowLongExits` / `AllowShortExits` | Enable or disable closing trades in a direction. | `true` |
| `StopLossPoints` | Stop-loss distance in price points. | `1000` |
| `TakeProfitPoints` | Take-profit distance in price points. | `2000` |
| `VolumeSource` | Volume stream (`Tick` or `Real`). Both use candle volume in this port. | `Tick` |
| `CandleType` | Timeframe for analysis. | `2h` |

## Candle Processing Workflow
1. Wait for a finished candle of the configured type.
2. Compute the CCI value and multiply it by the selected volume stream.
3. Feed the weighted CCI and the raw volume into the smoothing filters.
4. Once both smoothers are formed, determine the new color and update the history buffer.
5. Inspect the color at `SignalBar` and `SignalBar+1` to decide whether to close opposing positions and/or open a new trade.
6. Apply risk management via the preconfigured stop-loss and take-profit.

## Usage Notes
- The base `Strategy.Volume` must be set to a positive value; it defines the size of each entry.
- Because StockSharp candles do not expose tick counts, both `Tick` and `Real` volume modes use `candle.TotalVolume`. If tick-level data is required, feed the strategy with custom candles that encode tick volume in the `TotalVolume` field.
- The smoothing phase affects ALMA only. For other filters it is ignored, mirroring the behavior of the MQL indicator where certain modes disregard the phase input.
- Threshold multipliers (`HighLevel*` and `LowLevel*`) are retained for completeness. They can be visualized by plotting the smoothed volume and applying the multipliers externally if desired.

## Limitations & Differences vs. MQL5 Version
- StockSharp currently lacks direct implementations of VIDYA and Parabolic MA; EMA and ALMA are used as closest substitutes. This keeps the response characteristics similar but not identical to the original custom library.
- Order execution occurs immediately on the close of the signal candle. The MQL expert scheduled trades at the start of the next period via `TimeShiftSec`; this behavior is functionally equivalent when the broker executes market orders near instantly.
- Tick volume is approximated by total traded volume because individual tick counts are not exposed in standard candle messages.

## Getting Started
1. Attach the strategy to the desired `Security` and set `Volume` to the number of lots/contracts to trade per signal.
2. Choose the candle timeframe through `CandleType` (default: 2-hour time frame).
3. Adjust smoothing and risk parameters to match the target market’s volatility profile.
4. Run in paper mode first, review the charted smoothed oscillator, and fine-tune `SignalBar` if signals arrive too early or late.

## Optimization Ideas
- Optimize `SmoothingLength` alongside `CciPeriod` to align responsiveness with the target asset.
- Stress-test `SignalBar` around `0` and `1` for faster/slower reaction.
- Consider widening or tightening `StopLossPoints` / `TakeProfitPoints` to adapt to ATR of the instrument.
- Run the strategy on multiple timeframes and filter trades by higher timeframe trend direction if additional confirmation is needed.

## Safety Checklist
- Confirm that `Security.PriceStep` and `Volume` match the instrument’s contract specifications before live execution.
- Monitor slippage and adjust external risk controls if the chosen market is illiquid.
- Regularly review trade logs to ensure that direction filters (`Allow*`) align with the intended exposure.
