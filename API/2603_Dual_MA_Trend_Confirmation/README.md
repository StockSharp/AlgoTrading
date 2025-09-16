# Dual MA Trend Confirmation Strategy

## Overview
The **Dual MA Trend Confirmation Strategy** replicates the original MetaTrader expert that combines a slow exponential moving average (EMA) with a fast linear weighted moving average (LWMA). The system waits for both moving averages to align in the same direction and uses the previous candle close as additional confirmation before entering a position. The idea is to participate only in strong momentum swings when the slow trend filter and the fast confirmation filter simultaneously slope upward or downward.

The StockSharp implementation processes only fully finished candles, tracks the slope of each moving average over the last three bars, and automatically manages protective orders via the built-in `StartProtection` mechanism. The strategy is instrument-agnostic: it can operate on any security and timeframe that provide candles and supports the concept of “points” via the instrument price step.

## Indicators
- **Slow EMA** – Default period 57. Represents the dominant trend direction. The strategy requires the EMA to increase (or decrease) for two consecutive candles before trading.
- **Fast LWMA** – Default period 3. Acts as a momentum confirmation filter. Its slope must agree with the slow EMA, reinforcing that momentum supports the trend.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `SlowMaLength` | 57 | Period of the slow EMA trend filter. |
| `FastMaLength` | 3 | Period of the fast LWMA confirmation filter. |
| `StopLossPoints` | 100 | Protective stop distance expressed in instrument points (multiplied by `Security.PriceStep`). |
| `TakeProfitPoints` | 100 | Take-profit distance expressed in instrument points (multiplied by `Security.PriceStep`). |
| `CandleType` | 15-minute time frame | Candle data type used for all calculations. |

All parameters are exposed as `StrategyParam<T>` values so they can be modified at runtime or optimized through StockSharp’s optimization tools.

## Trading Rules
### Long Setup
1. Slow EMA is rising: current value > previous value > value two candles ago.
2. Fast LWMA is rising: current value > previous value > value two candles ago.
3. Previous candle close is above the previous slow EMA value.
4. Current slow EMA value is above the current fast LWMA value.
5. Current position is flat or short.
6. When all conditions are met, the strategy sends a market buy order for `Volume + |Position|` to flip into a long position.

### Short Setup
1. Slow EMA is falling: current value < previous value < value two candles ago.
2. Fast LWMA is falling: current value < previous value < value two candles ago.
3. Previous candle close is below the previous slow EMA value.
4. Current slow EMA value is below the current fast LWMA value.
5. Current position is flat or long.
6. When all conditions are met, the strategy sends a market sell order for `Volume + |Position|` to flip into a short position.

### Protective Logic
- `StartProtection` converts `StopLossPoints` and `TakeProfitPoints` into absolute price offsets by multiplying them with `Security.PriceStep`. Stop-loss and take-profit orders are issued as market exits so the engine can close the position even if limit orders are not supported.
- When the opposite signal appears, the strategy immediately reverses the position regardless of the protective orders.

## Implementation Details
- Only finished candles are processed, emulating the new-bar check from the original MQL version.
- The strategy keeps the last two moving average values and the previous close price in private fields to avoid indicator history lookups.
- `IsFormedAndOnlineAndAllowTrading()` ensures trading occurs only when all data streams are active and trading is permitted.
- Trade direction logs (`LogInfo`) provide transparency for debugging and live monitoring.
- Chart rendering (if available) draws candles and both moving averages for quick visual validation.

## Usage Notes
- Choose `Volume` according to the instrument lot size. The strategy always sends market orders sized `Volume + |Position|` to reverse efficiently.
- When running on instruments without a defined `PriceStep`, the code falls back to a value of `1`. Adjust parameters accordingly if tick size differs.
- Optimization can focus on the moving average periods and protective distances to adapt the strategy to different markets.
- Combine with additional filters (volatility, session times, etc.) if required. The modular structure makes it easy to extend.

## Suggested Optimization Ranges
- `SlowMaLength`: 20 – 120 with step 5–10.
- `FastMaLength`: 2 – 10 with step 1.
- `StopLossPoints` / `TakeProfitPoints`: 50 – 200 depending on instrument volatility.

These ranges closely mirror the original expert settings while providing flexibility for other instruments.
