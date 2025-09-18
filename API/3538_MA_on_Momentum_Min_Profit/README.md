# MA on Momentum Min Profit Strategy

## Overview
This strategy replicates the MetaTrader 5 expert advisor **MA on Momentum Min Profit.mq5** by trading the crossover between a Momentum indicator and a moving average that is calculated on top of the momentum series. A bullish signal appears when momentum crosses above its average while the previous bar kept momentum below the neutral 100 level. A bearish signal is generated when momentum crosses below the average with the previous bar above 100. The implementation keeps the original money based equity stop and the fixed take-profit distance measured in points.

## Trading logic
1. Request candles defined by `CandleType` and feed them into the Momentum indicator.
2. Smooth the momentum stream with a moving average defined by `MomentumMovingAverageType` and `MomentumMovingAveragePeriod`.
3. Detect crossovers using the previous bar values to avoid double signals.
4. Optional features from the MQL version:
   - Reverse the direction of the generated signals.
   - Close the opposite exposure before entering a new trade or skip the entry entirely.
   - Enforce a single net position at any time.
   - Allow triggering on the current (forming) candle instead of the fully closed bar.
5. Apply risk management:
   - Equity stop in money: `PnL + Position * (close - PositionPrice)` must remain above `StopLossMoney`.
   - Take-profit distance in points converted through `Security.PriceStep`.

## Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `CandleType` | `DataType` | `TimeSpan.FromMinutes(5).TimeFrame()` | Candles used to compute momentum. |
| `MomentumPeriod` | `int` | `14` | Lookback period of the Momentum indicator. |
| `MomentumMovingAveragePeriod` | `int` | `6` | Length of the moving average applied to momentum. |
| `MomentumMovingAverageType` | `MomentumMovingAverageType` | `Smoothed` | Moving average algorithm (Simple, Exponential, Smoothed, Weighted). |
| `ReverseSignals` | `bool` | `false` | Mirror MetaTrader buy/sell signals. |
| `CloseOpposite` | `bool` | `true` | Close the opposite exposure before opening a new position. |
| `OnlyOnePosition` | `bool` | `true` | Keep a single net position. |
| `UseCurrentCandle` | `bool` | `false` | Evaluate signals on the current forming candle instead of the closed bar. |
| `StopLossMoney` | `decimal` | `15` | Equity drawdown allowed before closing all trades. |
| `TakeProfitPoints` | `decimal` | `460` | Profit target in instrument points (multiplied by `PriceStep`). |
| `MomentumReference` | `decimal` | `100` | Neutral momentum level copied from the MQL strategy. |

## Implementation notes
- The moving average is implemented with `LengthIndicator<decimal>` instances to reuse StockSharp built-in SMA/EMA/SMMA/WMA classes.
- The original order queue and magic-number filters map to StockSharp net positions, therefore the strategy sends a single market order sized to both flatten the opposite side and open the new exposure when `CloseOpposite` is enabled.
- Equity protection closes all positions via `CloseAll()` once the floating loss breaches the threshold, exactly matching the MetaTrader behaviour of monitoring the combined commission, swap and profit.
