# Multi Indicator Optimizer Strategy

The strategy replicates the voting logic of the MetaTrader expert **MultiIndicatorOptimizer** on top of the high level StockSharp API. Five classic oscillators evaluate the finished candle and contribute a weighted vote toward the aggregated sentiment. The resulting score is then compared with user-defined thresholds to decide whether the strategy should go long, go short, or flatten an existing position.

## Trading logic

1. **MACD block** – inspects the sign of the histogram and the relation between the main and signal lines (both taken from the previous finished bar). The sum of these two signals is averaged and multiplied by `MacdWeight`.
2. **Awesome Oscillator block** – measures whether the oscillator is above or below the zero line and whether momentum improves versus the bar before. The average vote is scaled by `AoWeight`.
3. **OsMA block** – checks the sign of the MACD histogram from the previous candle and applies `OsmaWeight`.
4. **Williams %R block** – reacts to oversold/overbought crossings defined by `WilliamsLowerLevel` and `WilliamsUpperLevel`. A crossing upwards from the lower band votes bullish, while a crossing downwards from the upper band votes bearish. The result is multiplied by `WilliamsWeight`.
5. **Stochastic block** – combines two checks: a threshold crossing of %K vs. `StochasticLowerLevel`/`StochasticUpperLevel` and a %K/%D relationship. The average of both sub-signals is multiplied by `StochasticWeight`.

The aggregated score is stored in the `Signal` column of the logs and exposed via the `_lastSignal` field inside the strategy. The trading engine evaluates the score as follows:

- `signal >= EntryThreshold`: close any short position and open/maintain a long position.
- `signal <= -EntryThreshold`: close any long position and open/maintain a short position.
- `abs(signal) <= ExitThreshold`: flat the position to avoid trading in neutral market conditions.

All computations work on the previous finished candle to match the original MT4 implementation that used indexed indicator values (`shift = 1/2`).

## Parameters

| Parameter | Description | Default |
| --- | --- | --- |
| `CandleType` | Primary timeframe for all indicator calculations. | H1 candles |
| `MacdFast` / `MacdSlow` / `MacdSignal` | EMA lengths for the MACD block. | 12 / 26 / 9 |
| `MacdWeight` | Vote multiplier for the MACD block. Negative values invert the vote. | 1 |
| `AoShortPeriod` / `AoLongPeriod` | Moving-average lengths used by the Awesome Oscillator. | 5 / 34 |
| `AoWeight` | Vote multiplier for the Awesome block. | 1 |
| `OsmaFastPeriod` / `OsmaSlowPeriod` / `OsmaSignalPeriod` | MACD settings reused to build the OsMA histogram. | 12 / 26 / 9 |
| `OsmaWeight` | Vote multiplier for the OsMA block. | 1 |
| `WilliamsPeriod` | Lookback length for Williams %R. | 14 |
| `WilliamsLowerLevel` / `WilliamsUpperLevel` | Oversold/overbought boundaries (in percent). | -80 / -20 |
| `WilliamsWeight` | Vote multiplier for the Williams block. | 1 |
| `StochasticKPeriod` / `StochasticDPeriod` / `StochasticSlowing` | Periods for the Stochastic oscillator and its internal smoothing. | 5 / 3 / 3 |
| `StochasticLowerLevel` / `StochasticUpperLevel` | Oversold/overbought thresholds for %K. | 20 / 80 |
| `StochasticWeight` | Vote multiplier for the Stochastic block. | 1 |
| `EntryThreshold` | Minimum absolute vote required to open or reverse a position. | 0.5 |
| `ExitThreshold` | Neutral-zone width. Positions are closed when the absolute value of the signal falls below this value. | 0.1 |

All weights can be negative to suppress or invert the contribution of a block, which is convenient during optimization runs.

## Notes

- The strategy relies purely on the high level API: `SubscribeCandles`, indicator bindings, and `BuyMarket`/`SellMarket` helpers.
- Every indicator vote uses only completed candles, ensuring decisions are based on confirmed data.
- Position sizing is controlled by the base `Volume` property of `Strategy`. Protecting orders (stop loss / take profit) can be added externally via `StartProtection` if needed.
- Extensive comments are provided in English as requested to simplify further maintenance.
