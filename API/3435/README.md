# Meeting Lines Stochastic Strategy

## Overview

The **Meeting Lines Stochastic Strategy** is a StockSharp implementation of the MetaTrader expert *Expert_AML_Stoch*. It combines the Bullish/Bearish Meeting Lines candlestick reversal patterns with confirmation from the Stochastic oscillator's %D signal line. The strategy is designed for discretionary traders who want a rules-based approach to pattern recognition with additional momentum confirmation. By using the high-level StockSharp API, the code remains concise, testable, and easy to extend for portfolio management or further automation.

## Trading Logic

1. **Candlestick Pattern Filter**
   - The strategy continuously evaluates the last two completed candles to detect a Meeting Lines formation.
   - A bullish setup requires a long black candle followed by a long white candle whose closing price is within 10% of the previous close.
   - A bearish setup requires a long white candle followed by a long black candle with the same 10% close alignment.
   - The average candle body size is calculated with a configurable simple moving average to filter out weak bodies.

2. **Stochastic Confirmation**
   - The %D signal line of the Stochastic oscillator must confirm the candlestick signal.
   - Bullish entries demand that %D is below the configurable oversold threshold (default 30).
   - Bearish entries require %D to be above the configurable overbought threshold (default 70).

3. **Exit Rules**
   - Short positions are closed when %D crosses upward through either the lower exit level (default 20) or the upper exit level (default 80).
   - Long positions are closed when %D crosses downward through the same levels.
   - Reversal orders automatically close existing exposure and open a new position in the opposite direction.

4. **Volume Handling**
   - The strategy uses the base `Volume` property when it is positive; otherwise, it defaults to a single lot for compatibility with MetaTrader's fixed-lot behaviour.

## Parameters

| Name | Description | Default | Notes |
| ---- | ----------- | ------- | ----- |
| `CandleType` | Primary candle series used for analysis. | 15-minute time frame | Accepts any `DataType` supported by StockSharp. |
| `StochasticLength` | Lookback period for the raw %K calculation. | 3 | Mirrors the MetaTrader `%K period`. |
| `StochasticSmoothing` | Smoothing applied to %K (MetaTrader `slowing`). | 25 | Sets the internal smoothing length of the oscillator. |
| `StochasticSignal` | Smoothing period for the %D signal line. | 36 | Mirrors the MetaTrader `%D period`. |
| `BodyAveragePeriod` | Number of candles used to average the candle body size. | 3 | Filters out minor bodies when spotting Meeting Lines. |
| `LongEntryLevel` | Maximum %D value that still allows a bullish entry. | 30 | Equivalent to oversold threshold. |
| `ShortEntryLevel` | Minimum %D value required for a bearish entry. | 70 | Equivalent to overbought threshold. |
| `ExitLowerLevel` | Lower boundary that triggers exits on upward crosses. | 20 | Used for both long and short exit decisions. |
| `ExitUpperLevel` | Upper boundary that triggers exits on downward crosses. | 80 | Used for both long and short exit decisions. |

All parameters are exposed through `StrategyParam<T>` and can be optimised directly in StockSharp Designer or programmatically.

## Signal Generation

- **Long Entry**: Bullish Meeting Lines + %D below `LongEntryLevel` with no existing long exposure (shorts are reversed).
- **Short Entry**: Bearish Meeting Lines + %D above `ShortEntryLevel` with no existing short exposure (longs are reversed).
- **Long Exit**: %D crosses below `ExitUpperLevel` or `ExitLowerLevel`.
- **Short Exit**: %D crosses above `ExitLowerLevel` or `ExitUpperLevel`.

## Implementation Notes

- Indicator data is handled via `BindEx`, avoiding manual indicator collection management.
- Candle body averaging uses a `SimpleMovingAverage` fed with absolute body sizes through `DecimalIndicatorValue`, matching the MetaTrader helper `AvgBody`.
- All comments within the code are written in English, and indentation relies on tab characters in accordance with the project guidelines.
- The strategy automatically draws candles and the stochastic oscillator when a chart area is available, simplifying live monitoring.

## Usage Tips

1. **Optimisation**: Use the exposed parameters for walk-forward testing to align thresholds with the traded instrument.
2. **Risk Management**: Layer the strategy with StockSharp's built-in `StartProtection` or external portfolio-level risk controls for production deployments.
3. **Data Quality**: Meeting Lines patterns are sensitive to accurate open/close prices; ensure feed alignment and filtering of illiquid sessions.
4. **Time Frames**: Although the default is 15 minutes, intraday or daily data can be used by modifying `CandleType`.

The strategy offers a disciplined approach for traders who rely on candlestick formations but require oscillator confirmation to reduce false positives.
