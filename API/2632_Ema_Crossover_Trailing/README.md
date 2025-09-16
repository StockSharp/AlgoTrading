# EMA Crossover Trailing Strategy

## Overview
This strategy is a StockSharp port of the MQL5 expert advisor **"Intersection 2 iMA"**. It operates on two exponential moving averages (EMAs) and reacts to crossovers that occur on fully formed candles. The original expert was designed for MetaTrader 5 and managed trade volume dynamically; in this conversion the order size is controlled by a configurable parameter while preserving the crossover and trailing logic.

## Trading Logic
1. **Signal generation**
   - Calculate fast and slow EMAs on the selected candle series.
   - A **bullish crossover** (fast EMA crossing above slow EMA) triggers a buy signal when the previous candle closed with the fast EMA below or equal to the slow EMA and the current values show the fast EMA above the slow EMA.
   - A **bearish crossover** (fast EMA crossing below slow EMA) mirrors the rule above and produces a sell signal.
2. **Order execution**
   - When a buy signal is produced and no long position exists, the strategy sends a market buy order.
   - When a sell signal is produced and no short position exists, the strategy sends a market sell order.
   - If there is an opposite position, the order volume is increased to close the existing position before establishing the new one, matching the behaviour of the source EA that first closed opposite trades.
3. **Trailing stop management**
   - A stepped trailing stop maintains a fixed distance (in price steps) from the most favourable price.
   - The stop only moves when price has advanced by a user-defined step, preventing constant order modifications.
   - If the price violates the trailing level, the position is closed with a market order.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `FastPeriod` | Length of the fast EMA. | 4 |
| `SlowPeriod` | Length of the slow EMA. | 18 |
| `TrailingStopPoints` | Distance between market price and trailing stop in price steps (points). A value of `0` disables trailing. | 20 |
| `TrailingStepPoints` | Minimal progress in price steps before the trailing stop is moved forward. | 5 |
| `CandleType` | Candle data series used for calculations (time frame). | 15-minute candles |
| `TradeVolume` | Order size for market entries. | 1 |

## Implementation Notes
- The strategy uses the high-level `SubscribeCandles().Bind(...)` API to connect candle data with EMA indicators, ensuring no manual buffer management is necessary.
- Trailing distances are calculated by multiplying the configured number of points by the security `PriceStep`, replicating the digit-adjustment logic found in the MQL version.
- Trailing stops are implemented internally using market exits, because StockSharp does not expose the same `PositionModify` helper used in MetaTrader. The behaviour remains equivalent: once the trailing level is breached the position is exited immediately.
- Parameters are exposed through `StrategyParam<T>` so they can be optimised in the designer or adjusted from the UI.

## Usage Tips
- Align the `CandleType` with the time frame used in backtests or live trading to keep indicator values consistent.
- When trading instruments with small tick sizes, adjust `TrailingStopPoints` and `TrailingStepPoints` accordingly; the effective price distance equals *points × PriceStep*.
- Set `TradeVolume` to match the desired contract or lot size. The strategy automatically increases the order amount to close an opposite position when a new signal appears.

## Differences from the Original Expert Advisor
- Money management in MetaTrader used `MoneyFixedMargin`; the StockSharp version exposes a fixed order volume parameter instead, leaving advanced position sizing to outer configuration.
- The EA offered an unused `InpCloseHalf` input. It had no effect in the source code and was omitted.
- Stop trailing is handled internally rather than by modifying stop-loss orders, as this simplifies execution within StockSharp while keeping the exit logic identical.
