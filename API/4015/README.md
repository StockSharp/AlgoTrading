# Corrected Average Channel Strategy

## Overview
The **Corrected Average Channel Strategy** is a C# port of the MetaTrader expert advisor `e-CA-5`. The system rebuilds the "Corrected Average" (CA) indicator every time a candle closes and opens a position when price crosses the corrected moving average by a configurable sigma offset. The converted implementation relies on StockSharp's high-level candle API, uses market orders, and manages protective exits (stop-loss, take-profit, trailing stop) internally to mirror the behaviour of the original Expert Advisor.

## Corrected Average indicator
The CA filter combines a moving average with volatility feedback. The MQL version exposes three inputs: moving-average length, averaging method, and applied price. In the StockSharp port:

1. The moving average type is selected via the `MaTypeOption` parameter (SMA, EMA, SMMA, LWMA) and the `MaPeriod` length.
2. A `StandardDeviation` indicator with the same period measures current volatility.
3. For each finished candle the corrected value is computed iteratively:
   - Let `M_t` be the MA value on the latest bar and `CA_{t-1}` the corrected value from the previous bar.
   - Compute `v1 = StdDev_t^2` and `v2 = (CA_{t-1} - M_t)^2`.
   - If `v2 <= 0` or `v2 < v1`, keep the correction factor `k = 0`. Otherwise set `k = 1 - v1 / v2`.
   - Update `CA_t = CA_{t-1} + k * (M_t - CA_{t-1})`.
   - The very first corrected value defaults to the moving average itself.

This feedback loop dampens the MA during quiet periods and allows rapid adjustments when price diverges beyond the current volatility estimate.

## Trading logic
1. The strategy subscribes to the configured candle type (`CandleType`) and waits until both the moving average and the standard deviation are fully formed.
2. Once a candle finishes, the algorithm calculates the new corrected value and compares the previous candle close against the previous corrected level.
3. Two sigma offsets, `SigmaBuyPoints` and `SigmaSellPoints`, are converted into price distances using the instrument's `PriceStep`.
4. Entry rules use the previous candle close and the freshly computed corrected level:
   - **Buy** if the previous close was below the corrected average plus the buy sigma, and the current close finishes above that upper boundary.
   - **Sell** if the previous close was above the corrected average minus the sell sigma, and the current close finishes below that lower boundary.
5. Only one net position is allowed. A new trade is submitted only when no exposure is present.

Because the StockSharp version operates on finished candles, the breakout confirmation happens once per bar instead of on every tick, providing deterministic behaviour suitable for backtesting and live automation with candle data.

## Risk management
The port reproduces all three protective mechanics from the original Expert Advisor:

- **Fixed stop-loss**: `StopLossPoints` multiplied by the price step defines the distance between the entry price and the protective stop. A triggered stop closes the entire position with a market order.
- **Fixed take-profit**: `TakeProfitPoints` converts to a profit target distance. When price reaches the level during a candle, the position is closed with a market order.
- **Trailing stop**: When `TrailingPoints` is greater than zero the strategy tracks unrealised profit and, once the price has advanced by at least that distance, stores a trailing level behind the latest close. The trailing stop only moves forward and honours `TrailingStepPoints`, which represents the minimum improvement before a new trailing level is accepted. Trailing levels are rounded with `Security.ShrinkPrice` so they align with the instrument's tick size.

All exits reset the internal risk state. When the next signal appears the stop, target, and trailing levels are recalculated from the new fill price, ensuring behaviour close to the MQL version that modifies the original order protections.

## Parameters
| Parameter | Description |
| --- | --- |
| `OrderVolume` | Quantity used for market entries. Must be positive. |
| `TakeProfitPoints` | Profit target in price steps (0 disables the take-profit). |
| `StopLossPoints` | Stop-loss distance in price steps (0 disables the stop-loss). |
| `TrailingPoints` | Profit distance (in price steps) required before the trailing stop activates. |
| `TrailingStepPoints` | Minimum extra distance that must be captured before moving the trailing stop again. |
| `MaPeriod` | Period of both the moving average and the standard deviation. |
| `MaTypeOption` | Moving average type: SMA, EMA, SMMA, or LWMA. |
| `SigmaBuyPoints` | Sigma offset added above the corrected average before opening a long position. |
| `SigmaSellPoints` | Sigma offset subtracted below the corrected average before opening a short position. |
| `CandleType` | Candle series used for indicator calculations and signal evaluation. |

All numeric parameters support optimisation through `SetCanOptimize(true)` so the strategy can be calibrated directly inside the StockSharp environment.

## Usage notes
- The default candle type is one hour. Adjust it to match the timeframe that was used when optimising the original MetaTrader strategy.
- `Security.PriceStep` is used to translate all "points" inputs to actual price distances. Instruments without a configured step fall back to `1`, preserving sensible behaviour for indexes or cryptocurrencies.
- The strategy executes only on finished candles. If intrabar precision is required, lower the timeframe to the desired granularity.
- Trailing stops are implemented with market orders when violated, mimicking the original EA that modified stop-loss prices. This approach avoids placing additional stop orders and keeps risk management contained within the strategy itself.
- No Python version is provided for this conversion, per the task requirements.

## Differences from the original EA
- StockSharp's candle-based API replaces tick-level processing; all decisions are taken when a candle closes.
- Order management is netted: opposing positions are not held simultaneously, matching the single-order logic of the MetaTrader version.
- Protective stops and trailing exits are executed via market orders instead of modifying existing order tickets. This behaviour is equivalent on netting accounts while keeping the implementation consistent with other StockSharp strategies.

These adaptations preserve the trading idea of `e-CA-5` while aligning the logic with StockSharp best practices and the high-level API conventions described in the repository guidelines.
