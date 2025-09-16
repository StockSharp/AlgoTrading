# JS Sistem 2 Strategy

## Overview
JS Sistem 2 is a trend-following system originally written for MetaTrader 5. The StockSharp port keeps the multi-indicator confirmation block from the expert advisor and trades on closed candles of the selected timeframe. Orders are sized with a fixed volume and can optionally be blocked if the connected portfolio balance falls below a configurable threshold. Risk is controlled through hard stop-loss and take-profit distances expressed in pips together with an adaptive trailing stop that follows candle shadows.

## Indicators and Filters
- **EMA(55), EMA(89), EMA(144)** – form a directional filter. Long setups require the fast EMA above the medium and the medium above the slow line, while the distance between the fast and slow curves must remain below `MinDifferencePips`.
- **MACD histogram (OsMA)** – uses fast, slow, and signal EMA lengths identical to the MQL version. A long trade requires the histogram to be positive, a short trade requires it to be negative.
- **Relative Vigor Index (RVI)** – computed with period `RviPeriod` and smoothed by an additional simple moving average with `RviSignalLength`. Long trades need the RVI to stay above its signal line and above the `RviMax` threshold; short trades need the inverse.
- **Highest/Lowest swing envelopes** – track the highest high and lowest low over `VolatilityPeriod` candles. These values drive the trailing stop logic and replicate the shadow trailing mode from the original expert advisor.

## Trade Logic
1. The strategy processes only finished candles from the configured `CandleType`.
2. Before evaluating entries it updates the trailing stop for existing positions using the latest swing extremes and then checks whether stop-loss or take-profit levels were hit during the candle.
3. Long entry conditions:
   - Portfolio balance is above `MinBalance`.
   - EMA55 > EMA89 > EMA144 and the difference between EMA55 and EMA144 is below `MinDifferencePips` (converted into price units through the instrument pip size).
   - MACD histogram (`macdLine`) is greater than zero.
   - RVI is above its signal line and the signal line is at or above `RviMax`.
   - No existing long position (`Position <= 0`). When a short position exists it is flattened before opening the long.
4. Short entry conditions mirror the long rules with inverted comparisons and use the `RviMin` threshold.
5. Upon entry the strategy stores the candle close price as the reference, places virtual stop-loss and take-profit levels by shifting that price by `StopLossPips` and `TakeProfitPips`, and resets the trailing state.

## Exit and Trailing Management
- **Hard stop-loss / take-profit:** Whenever the candle range overlaps the stored stop or target level the strategy closes the entire position immediately.
- **Trailing stop:** When `TrailingEnabled` is true, the strategy attempts to move the stop in the direction of profit. For longs the stop is raised to the lowest low of the last `VolatilityPeriod` candles once that low sits above both the entry price and the previous stop by at least `TrailingIndentPips`. Shorts follow the symmetric rule using the highest high. This reproduces the “shadow trailing” of the MQL advisor and keeps stops from tightening prematurely.
- **Balance protection:** If the current portfolio value drops below `MinBalance` the strategy refrains from submitting new orders but still manages open trades and trailing stops.

## Parameters
| Parameter | Description | Default |
| --- | --- | --- |
| `MinBalance` | Minimum portfolio balance required for new entries. | 100 |
| `Volume` | Order volume submitted with each trade. | 1 |
| `StopLossPips` | Stop-loss distance measured in pips. Set to 0 to disable. | 35 |
| `TakeProfitPips` | Take-profit distance measured in pips. Set to 0 to disable. | 40 |
| `MinDifferencePips` | Maximum allowed spread between the fast and slow EMA in pips. | 28 |
| `VolatilityPeriod` | Number of candles used to compute swing highs and lows for the trailing stop. | 15 |
| `TrailingEnabled` | Enables or disables the trailing stop logic. | true |
| `TrailingIndentPips` | Minimum gap between price, entry, and stop when updating the trailing stop. | 1 |
| `MaFastPeriod` | Period for the fast EMA. | 55 |
| `MaMediumPeriod` | Period for the medium EMA. | 89 |
| `MaSlowPeriod` | Period for the slow EMA. | 144 |
| `OsmaFastPeriod` | Fast EMA length for the MACD histogram. | 13 |
| `OsmaSlowPeriod` | Slow EMA length for the MACD histogram. | 55 |
| `OsmaSignalPeriod` | Signal smoothing length for the MACD histogram. | 21 |
| `RviPeriod` | Period of the Relative Vigor Index. | 44 |
| `RviSignalLength` | Length of the SMA applied to the RVI to obtain its signal line. | 4 |
| `RviMax` | Upper bound that the RVI signal must reach before long entries are allowed. | 0.04 |
| `RviMin` | Lower bound that the RVI signal must reach before short entries are allowed. | -0.04 |
| `CandleType` | Timeframe of the candles used for all calculations. | 5-minute candles |

## Implementation Notes
- Pip distance is derived from the instrument’s price step. Instruments quoted with 3 or 5 decimal places use a pip equal to ten price steps, matching the original MQL logic.
- Stop and target handling happens inside the strategy loop because StockSharp does not automatically submit server-side orders for them in this template.
- The strategy calls `StartProtection()` during startup so the base class can monitor unexpected disconnections and pending positions.
