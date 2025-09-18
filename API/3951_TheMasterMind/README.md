# The MasterMind Strategy (StockSharp Port)

## Overview
- Port of the MetaTrader 4 expert advisor "TheMasterMind" that combines a Stochastic oscillator with Williams %R to capture extreme reversals.
- Implemented with StockSharp's high-level API using candle subscriptions and indicator bindings.
- Trades a single security and reacts on finished candles only, mirroring the original "trade at close" execution style.

## Trading Logic
1. **Indicator preparation**
   - `StochasticOscillator` delivers the %D signal line with configurable %K/%D smoothing and total lookback length.
   - `WilliamsR` measures the relative location of the close within the recent high/low range.
2. **Entry rules**
   - **Buy** when `%D <= 3` _and_ `Williams %R <= -99.5`, signalling a stochastic oversold extreme together with a deep WPR penetration below the lower bound.
   - **Sell** when `%D >= 97` _and_ `Williams %R >= -0.5`, signalling an overbought extreme confirmed by Williams %R staying close to 0.
   - If an opposite position exists it is flattened first, then a new market order is sent with the configured base volume.
3. **Exit rules**
   - Reverse signals close the current position and flip the direction (one position at a time, matching the hedging-disabled mode used in the MQL script).
   - Optional `StartProtection` stop-loss, take-profit and trailing stop services handle protective exits exactly once per strategy start.

## Risk Management
- Parameters `StopLoss`, `TakeProfit`, `UseTrailingStop`, `TrailingStop`, and `TrailingStep` map to the original EA's money-management controls.
- All distances are expressed in absolute price units to stay broker-agnostic. Leave them at `0` to disable the respective protection feature.
- `StartProtection` is activated automatically when at least one of the protective distances is non-zero.

## Strategy Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `TradeVolume` | Base lot size for each fresh entry. | `1` |
| `StochasticPeriod` | Total lookback for the stochastic oscillator. | `100` |
| `KPeriod` | %K smoothing length. | `3` |
| `DPeriod` | %D signal length. | `3` |
| `WilliamsPeriod` | Lookback length for Williams %R. | `100` |
| `StochasticBuyThreshold` | Upper bound that %D must stay below to allow longs. | `3` |
| `StochasticSellThreshold` | Lower bound that %D must stay above to allow shorts. | `97` |
| `WilliamsBuyLevel` | Oversold level for Williams %R. | `-99.5` |
| `WilliamsSellLevel` | Overbought level for Williams %R. | `-0.5` |
| `StopLoss` | Absolute stop-loss distance. | `0` |
| `TakeProfit` | Absolute take-profit distance. | `0` |
| `UseTrailingStop` | Enables trailing protection when `true`. | `false` |
| `TrailingStop` | Absolute trailing stop distance. | `0` |
| `TrailingStep` | Step applied while trailing. | `0` |
| `CandleType` | Timeframe for the primary candle subscription (default 15 minutes). | `15m time frame` |

## Implementation Notes
- The strategy subscribes to a single candle series via `SubscribeCandles(CandleType)` and binds the stochastic and Williams %R indicators using `BindEx`.
- Trading decisions are taken only when `candle.State == CandleStates.Finished` and `IsFormedAndOnlineAndAllowTrading()` is satisfied.
- Chart helpers (`DrawCandles`, `DrawIndicator`, `DrawOwnTrades`) are invoked when a chart area is available to visualize the indicators and trades.
- Logging statements (`LogInfo`) mirror the original alert strings, helping to trace the decision process during live trading or backtesting.
