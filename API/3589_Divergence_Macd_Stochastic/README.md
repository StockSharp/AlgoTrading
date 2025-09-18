# Divergence MACD Stochastic Strategy

This strategy recreates the MetaTrader 5 expert advisor **"Divergence EA pip sl tp"** in the StockSharp framework. The algorithm searches for classical divergences between price action and the MACD histogram, then validates the signal with an overbought/oversold Stochastic oscillator filter before opening reversal trades.

## Trading logic

1. Subscribe to the primary timeframe candles selected by the `CandleType` parameter.
2. Calculate the MACD histogram (`MACD line - Signal line`) and the Stochastic %K/%D values on every finished candle.
3. Track the latest two swing highs and lows of both price and histogram values.
4. **Bearish divergence**: a new higher price high accompanied by a lower MACD histogram peak and Stochastic %K above `StochasticUpperLevel` triggers a short position or reverses an existing long.
5. **Bullish divergence**: a new lower price low with a higher MACD histogram trough and %K below `StochasticLowerLevel` opens or reverses into a long position.
6. Optional protective `TakeProfitSteps` and `StopLossSteps` are converted to StockSharp step units and activated once when the strategy starts.

## Implementation notes

- Built with StockSharp high-level API using a single candle subscription bound to `MovingAverageConvergenceDivergenceSignal` and `StochasticOscillator` indicators.
- Maintains divergence state without calling indicator `GetValue` helpers, complying with the conversion guidelines.
- Chart integration displays price candles, MACD, and Stochastic lines when a chart area is available.
- Positions are reversed by adding the absolute current position size to the base `Volume`, ensuring immediate direction changes after confirmed divergences.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `CandleType` | Timeframe used for divergence calculations. | 1-hour candles |
| `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` | MACD EMA lengths replicating the original EA inputs. | 12 / 26 / 9 |
| `MacdDivergenceThreshold` | Minimum histogram difference between consecutive swings required to confirm divergence. | 0.0005 |
| `StochasticLength` | Fast %K period of the Stochastic oscillator. | 50 |
| `StochasticSlowK`, `StochasticSlowD` | Additional %K/%D smoothing lengths mirroring the EA configuration. | 9 / 9 |
| `StochasticUpperLevel`, `StochasticLowerLevel` | Overbought and oversold filters validating bearish/bullish setups. | 80 / 20 |
| `TakeProfitSteps`, `StopLossSteps` | Optional protection distances expressed in price steps (0 disables the level). | 50 |

## Usage

1. Attach the strategy to a StockSharp connector with a security supporting the selected timeframe.
2. Configure position size through the base `Volume` property and adjust indicator settings as desired.
3. Start the strategy—orders will be generated automatically whenever the divergence and Stochastic conditions are satisfied.
