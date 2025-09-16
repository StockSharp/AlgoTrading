# Burg Extrapolator Strategy

## Overview
The Burg Extrapolator Strategy is a StockSharp port of the MetaTrader 4 expert advisor "Burg Extrapolator". The original system fits a Burg autoregressive (AR) model to a sliding window of open prices (or their momentum/ROC transforms) and projects a path of future prices. Trading decisions are derived from the most extreme forecast values: if the predicted excursion in one direction is large enough the strategy either stacks new positions or liquidates exposure in the opposite direction. The conversion keeps the same modelling blocks while mapping position management and money management to StockSharp primitives.

## Trading Logic
1. **Data preparation**
   - Build a rolling history of `PastBars + 1` open prices for the selected `CandleType`.
   - Optionally transform the data into logarithmic momentum (default) or percentage rate of change before feeding it to the AR model. Raw prices are centered by their moving average to mirror the MT4 code.
2. **Burg linear prediction**
   - Estimate reflection coefficients up to the order `PastBars * ModelOrder` using the Burg algorithm.
   - Generate a sequence of future values (`PastBars` steps ahead in practice) by recursively expanding the AR model. Transforms are inverted back to price space so that all forecasts operate in absolute price units.
3. **Signal detection**
   - Walk through the forecast path and record the highest and lowest predicted price before another extreme appears. The distance between the first extreme and the other side of the forecast range is compared with `MaxLoss` and `MinProfit` thresholds (converted to absolute price by multiplying with the instrument `PriceStep`).
   - A sufficiently large upswing triggers `OpenSignal = 1` while a large downswing yields `OpenSignal = -1`. If the opposing extreme appears first the logic sets `CloseSignal` to exit current exposure even if no fresh entry is planned.
4. **Order management**
   - Protective exits (stop-loss, take-profit, and optional trailing-stop) run before any new signal is executed. The trailing-stop reuses the best price since the last entry and closes the position when the price retraces by `TrailingStop` points, matching the MT4 behaviour of moving the protective order.
   - If a signal asks to close exposure in the opposite direction the strategy sends a market order sized to flatten the current net position.
   - Entry signals stack additional market orders in the indicated direction until `MaxTrades` is reached. Order volume scales linearly with the number of active trades using the factor `1 + existingTrades * MaxRisk`, a StockSharp-friendly replacement for the original margin-based sizing routine.

## Indicators and Data
- Candle subscription defined by `CandleType` (default 30-minute time frame).
- Internal Burg autoregressive model (implemented without external indicators).
- Optional logarithmic momentum and percentage rate of change transforms.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `CandleType` | 30-minute candles | Primary timeframe processed by the strategy. |
| `MaxRisk` | 0.5 | Risk multiplier used when stacking multiple trades. |
| `MaxTrades` | 5 | Maximum number of simultaneous trades per direction. |
| `MinProfit` | 160 | Minimum predicted profit (in points) required to open new trades. |
| `MaxLoss` | 130 | Maximum tolerated forecasted loss (in points) before closing trades. |
| `TakeProfit` | 0 | Optional fixed take-profit distance in points (0 disables it). |
| `StopLoss` | 180 | Optional fixed stop-loss distance in points (0 disables it). |
| `TrailingStop` | 10 | Trailing stop distance in points, active only when `StopLoss > 0`. |
| `PastBars` | 200 | Number of historical candles used by the Burg model. |
| `ModelOrder` | 0.37 | Fraction of `PastBars` converted into the Burg order. |
| `UseMomentum` | true | Apply logarithmic momentum transform to input data. |
| `UseRateOfChange` | false | Apply percentage rate of change (ignored when momentum is enabled). |

All parameters are `StrategyParam<T>` instances and can be optimised or adjusted in the StockSharp Designer.

## Implementation Notes
- The Burg algorithm is implemented directly in C# and keeps the same recursion as the MT4 version. All computations are executed in double precision while the final forecasts are converted back to `decimal` before signal checks.
- The original EA could rely on MetaTrader account information to size positions. In StockSharp the money management block is replaced with a deterministic scaling rule based on `Volume` and `MaxRisk`. Set `Volume` to the desired base lot and the strategy will scale subsequent entries proportionally.
- Protective logic closes positions with explicit market orders instead of modifying broker-side stops; this matches StockSharp's high-level API design and prevents stale state when running in simulation.
- The forecast arrays are re-created whenever `PastBars` or `ModelOrder` change so on-the-fly parameter edits immediately affect the AR model without restarting the strategy.
- To visualise the behaviour you can attach a chart in Designer: the strategy already draws candles and executed trades on the default area. Extending the sample with custom series (e.g., forecast path) is straightforward if desired.
