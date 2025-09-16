# EA Close Strategy

## Overview
The **EA Close Strategy** is a direct StockSharp port of the original MQL5 expert advisor "EA Close" created by Vladimir Karputov. The strategy combines a Commodity Channel Index (CCI), a weighted moving average (WMA), and a Stochastic oscillator to detect exhaustion moves at the end of retracements. Orders are evaluated only once per completed candle to mimic the "new bar" logic used in the source EA.

The StockSharp implementation keeps the parameter set and structure of the MQL version so that existing optimizations can be reused. Signals are generated from the previous completed candle, which makes the behaviour deterministic when the strategy is replayed on historical data.

## Indicators
- **Commodity Channel Index (CCI)** – identifies overbought and oversold extremes relative to the average price over a configurable period.
- **Weighted Moving Average (WMA)** – acts as a micro trend filter; the original EA uses a 1-period LWMA of the weighted price, which in practice behaves like a slightly smoothed candle price. In this port the WMA is applied to the candle stream directly.
- **Stochastic Oscillator (%K line)** – confirms momentum exhaustion using classic overbought and oversold levels.

## Trading Logic
1. **Long setup**
   - Previous candle's CCI crosses below `-CciLevel`.
   - Previous Stochastic %K is below `StochasticLevelDown`.
   - Previous candle open is above the WMA value of that candle.
   - If those conditions align and the current net position is non-positive, the strategy buys. Existing short exposure is netted before opening the new long.
2. **Short setup**
   - Previous candle's CCI rises above `CciLevel`.
   - Previous Stochastic %K is above `StochasticLevelUp`.
   - Previous candle close is below the WMA value of that candle.
   - When true and the position is non-negative, the strategy sells. Any open long position is closed in the same market order.

Only finalized candle data are used. This mirrors the `OnTick` new-bar gate in the MQL script and prevents intrabar repainting.

## Risk Management
`StartProtection` is enabled during `OnStarted`, reproducing the fixed stop-loss and take-profit distances from the MQL code. Distances are configured in **pips**. The helper converts pips to price units by multiplying the instrument's price step by 10 when the step precision has three or five decimal places (e.g., 0.001 or 0.00001), matching the EA's digit adjustment for 3/5-digit quotes. Setting a distance to zero disables that protection leg.

## Parameters
| Name | Description | Default |
| ---- | ----------- | ------- |
| `Volume` | Order size used for market entries. | 1 |
| `StopLossPips` | Fixed stop-loss distance measured in pips. | 35 |
| `TakeProfitPips` | Fixed take-profit distance measured in pips. | 75 |
| `CciPeriod` | Averaging length of the CCI indicator. | 14 |
| `CciLevel` | Absolute threshold that defines CCI extremes. | 120 |
| `MaPeriod` | Length of the weighted moving average filter. | 1 |
| `StochasticLength` | Lookback window for the stochastic oscillator (highest/lowest range). | 5 |
| `StochasticKPeriod` | Smoothing factor applied to the %K line. | 3 |
| `StochasticDPeriod` | Smoothing factor applied to the %D line. | 3 |
| `StochasticLevelUp` | Overbought threshold for the %K line. | 70 |
| `StochasticLevelDown` | Oversold threshold for the %K line. | 30 |
| `CandleType` | Candle series used as the data source. | 1-hour time frame |

## Usage Notes
- The strategy stores indicator and price values from the most recent finished candle and evaluates signals on the next bar open, replicating the array-shift logic (`CopyBuffer(..., start=1)`) in the EA.
- Market orders are sized to flatten any opposite exposure and simultaneously open the new position, closely matching the `ClosePositions` helper in MQL.
- The StockSharp `StochasticOscillator` uses `Length` as the lookback window, `KPeriod` for %K smoothing, and `DPeriod` for %D smoothing, equivalent to the `iStochastic` parameters (K-period, slowing, and D-period respectively).
- Because StockSharp works with aggregated candles instead of tick callbacks, no additional rate refresh logic is required—the data subscription ensures the indicators receive complete candles.

## Conversion Notes
- No Python implementation is provided intentionally, aligning with the conversion task requirements.
- The weighted moving average operates on the candle series; if you need the exact MT5 weighted price `(High + Low + 2 * Close) / 4`, pre-process the candle values before feeding them into the WMA.
- Protective orders are managed by the platform via `StartProtection`, so explicit stop/take registrations after each trade are not necessary.
