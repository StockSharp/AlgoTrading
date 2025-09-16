# OHLC Stochastic Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Momentum-following strategy that uses the classic %K/%D stochastic oscillator on OHLC candles.
The algorithm reacts to crossovers in oversold/overbought zones and protects open trades with a configurable trailing stop measured in price steps.

## Details

- **Core Idea**: exploit the shift in momentum when stochastic %K crosses %D at extreme levels.
- **Entry Criteria**:
  - **Long**:
    - %K crosses above %D and at least one of the lines is below the `LevelDown` threshold.
    - If a short position exists it is closed and reversed to long.
  - **Short**:
    - %K crosses below %D and at least one of the lines is above the `LevelUp` threshold.
    - If a long position exists it is closed and reversed to short.
- **Exit Criteria**:
  - Trailing stop is hit (based on `TrailingStopSteps` distance and `TrailingStepSteps` improvement requirement).
  - Opposite entry signal appears, triggering a reversal.
- **Trailing Logic**:
  - Distance and step are multiplied by the security `PriceStep` to convert pips/steps into absolute prices.
  - Stop only advances after the trade moves beyond `TrailingStopSteps + TrailingStepSteps` from the entry price.
  - Separate trailing logic for long and short sides.
- **Indicators**:
  - [StochasticOscillator](https://doc.stocksharp.com/html/T_StockSharp_Algo_Indicators_StochasticOscillator.htm) with adjustable `KPeriod`, `DPeriod`, and `Slowing`.
- **Long/Short**: Both.
- **Stops**: Trailing stop only (no fixed SL/TP orders).
- **Position Sizing**: Uses the strategy `Volume` parameter; reversals send `Volume + |Position|` to flip direction.
- **Default Parameters**:
  - `CandleType` = `TimeSpan.FromHours(12).TimeFrame()`
  - `KPeriod` = 5
  - `DPeriod` = 3
  - `Slowing` = 3
  - `LevelUp` = 70
  - `LevelDown` = 30
  - `TrailingStopSteps` = 5 (price steps)
  - `TrailingStepSteps` = 2 (price steps)
- **Visualization**:
  - Draws OHLC candles, stochastic indicator, and trade markers when charts are available.

## Usage Notes

1. Configure the underlying security and timeframe before starting the strategy.
2. Adjust `TrailingStopSteps` according to the instrument tick size to reflect real pip distances.
3. The strategy calls `StartProtection()` so additional risk rules can be attached externally.
4. Works best on trending regimes where stochastic reversals lead price.
5. For intraday products lower timeframes may require reducing trailing distances to avoid premature exits.
