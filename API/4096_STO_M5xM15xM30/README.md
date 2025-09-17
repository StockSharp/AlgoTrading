# STO M5xM15xM30 Strategy

## Overview
This strategy is a faithful C# conversion of the MetaTrader 4 expert advisor "STO_m5xm15xm30". It uses three stochastic oscillators computed on M5, M15, and M30 timeframes to identify synchronized momentum shifts. The StockSharp implementation keeps the original entry/exit structure, replaces manual order management with the high-level API, and exposes every key constant as a configurable `StrategyParam`.

## Trading Logic
1. **Multi-timeframe confirmation**
   - The primary (default M5) stochastic must show a bullish crossover (`%K` crosses above `%D`).
   - The middle (default M15) and slow (default M30) stochastic values must already be bullish (`%K` above `%D`).
   - A bearish setup requires the mirrored conditions (`%K` below `%D`).
2. **Shift filter**
   - The primary stochastic also checks the state `ShiftBars` candles earlier. A buy signal requires the historical `%K` to be below `%D`, ensuring a fresh crossover. Sell signals require the opposite.
3. **Price momentum filter**
   - The latest close must be higher (for buys) or lower (for sells) than the previous completed candle close. This mirrors the `Close[0] > Close[1]` rule from the MT4 script.
4. **Entry rules**
   - If no position is open and the bullish criteria are met, the strategy opens a long market order with the configured `TradeVolume`.
   - If a short position exists when a bullish signal arrives, it is flattened first and a long position is opened afterwards. The inverse is true for bearish signals.
5. **Exit rules**
   - A dedicated M5 stochastic with period `ExitKPeriod` checks the previous candle (`shift = 1`). A long position is closed when `%K` falls below `%D`; a short is closed when `%K` rises above `%D`.
   - After an exit is triggered, the strategy skips immediate re-entry on the same candle, replicating the MT4 order loop behaviour.

## Indicators and Data Subscriptions
- Primary candles: default 5-minute timeframe (`CandleType`).
- Middle confirmation candles: default 15-minute timeframe (`MiddleCandleType`).
- Slow confirmation candles: default 30-minute timeframe (`SlowCandleType`).
- Stochastic oscillators: all use %K smoothing = 3 and %D smoothing = 3, matching the original parameters.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `CandleType` | 5-minute candles | Working timeframe for entries and exits. |
| `MiddleCandleType` | 15-minute candles | Confirmation timeframe #1. |
| `SlowCandleType` | 30-minute candles | Confirmation timeframe #2. |
| `FastKPeriod` | 5 | %K period for the primary stochastic. |
| `MiddleKPeriod` | 5 | %K period for the middle stochastic. |
| `SlowKPeriod` | 5 | %K period for the slow stochastic. |
| `ExitKPeriod` | 5 | %K period for the exit stochastic that operates on the previous bar. |
| `ShiftBars` | 3 | Number of bars between the reference crossover and the current bar. |
| `TakeProfitPoints` | 30 | Protective take-profit distance (points). |
| `StopLossPoints` | 10 | Protective stop-loss distance (points). |
| `TradeVolume` | 0.1 | Order volume used for new entries. |

All parameters are exposed through `StrategyParam<T>`, making them available for optimization inside StockSharp Designer.

## Risk Management
`StartProtection()` translates the MT4 `TP` and `SL` inputs into StockSharp protective orders. Both can be disabled by setting the corresponding parameter to zero.

## Implementation Notes
- Indicator values are obtained exclusively through `SubscribeCandles(...).BindEx(...)`, complying with the high-level API guidelines and avoiding manual indicator collections.
- The `StochasticShiftBuffer` helper mimics the MT4 `shift` argument without calling `GetValue`, keeping only the necessary bar history.
- Entry processing happens once per completed candle. Exit evaluation occurs before entry logic, matching the original EA's processing order.
- Inline comments explain each processing step and clarify how the MQL logic maps to the StockSharp code.

## Usage
1. Add the strategy to a StockSharp scheme or designer project.
2. Configure the desired symbol and ensure historical data for M5, M15, and M30 candles is available.
3. Adjust the parameters to suit the target market or optimization scenario.
4. Start the strategy; protective stop-loss/take-profit levels are registered automatically for every position.
