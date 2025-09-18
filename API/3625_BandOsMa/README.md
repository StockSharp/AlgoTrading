# BandOsMa Strategy

## Overview
The **BandOsMa Strategy** converts the MetaTrader 5 "BandOsMA" expert advisor into a StockSharp strategy. It evaluates the MACD histogram (OsMA) using Bollinger Bands built directly on the histogram values. Breakouts above or below the bands create entry signals, while an additional moving average of the histogram manages signal exits.

The strategy operates on a single symbol and timeframe selected by the user. Indicator values are calculated on finished candles using StockSharp's high-level candle subscriptions.

## Trading Logic
1. **Indicators**
   - `MovingAverageConvergenceDivergenceSignal` provides the MACD histogram (OsMA).
   - `BollingerBands` is applied to the OsMA sequence to detect extreme deviations.
   - A configurable moving average smooths the histogram and acts as an exit filter.
2. **Entry**
   - A **long signal** appears when the current OsMA closes below the lower band while the previous bar stayed above it.
   - A **short signal** appears when the current OsMA closes above the upper band while the previous bar stayed below it.
3. **Exit**
   - Signals are cleared when the histogram crosses the moving average in the opposite direction.
   - When an open position no longer matches the active signal, the position is closed immediately.
   - A pip-based stop-loss is attached to each position. The stop also acts as a trailing stop with the same distance and a trailing step equal to `StopLossPoints / 50` (mirroring the MetaTrader helper class).

## Position Management
- **Stop Loss & Trailing**: The stop distance is expressed in MetaTrader points and converted into price units using the instrument's `PriceStep`. The same distance is used for the trailing stop, which moves forward once the close price improves by at least the trailing step.
- **One Position at a Time**: Only one net position is maintained. Opposite signals close the current position before considering a new entry.

## Parameters
| Group | Name | Description | Default |
| --- | --- | --- | --- |
| General | `CandleType` | Timeframe for candle subscription and indicator calculation. | `H1` |
| Risk | `LotSize` | Trade volume in lots. | `0.01` |
| Risk | `StopLossPoints` | Stop-loss distance expressed in MetaTrader points (also used for trailing). | `1000` |
| Indicators | `MacdFastPeriod` | Fast EMA length in MACD. | `12` |
| Indicators | `MacdSlowPeriod` | Slow EMA length in MACD. | `26` |
| Indicators | `MacdSignalPeriod` | Signal EMA length in MACD. | `9` |
| Indicators | `PriceType` | Applied price for MACD input (`Close`, `Open`, `High`, `Low`, `Median`, `Typical`, `Weighted`). | `Typical` |
| Indicators | `BollingerPeriod` | Period of Bollinger Bands over the OsMA sequence. | `26` |
| Indicators | `BollingerShift` | Shift applied to Bollinger buffers (non-negative). | `0` |
| Indicators | `BollingerDeviation` | Standard deviation multiplier for Bollinger Bands. | `2` |
| Indicators | `MovingAveragePeriod` | Length of the moving average applied to OsMA. | `10` |
| Indicators | `MovingAverageShift` | Shift applied to the moving average buffer (non-negative). | `0` |
| Indicators | `MovingAverageMethod` | Moving average type (`Simple`, `Exponential`, `Smoothed`, `LinearWeighted`). | `Simple` |

## Implementation Notes
- Candle processing uses `WhenCandlesFinished` to ensure only final bars drive the logic.
- Indicator values are stored in history buffers to emulate MetaTrader-style buffer shifts. Negative shifts are not supported; use zero or positive values as in the original expert defaults.
- Trailing stops rely on candle closes rather than tick-by-tick updates. Adjust the pip distance if precise tick-level trailing is required.

## Usage
1. Select the desired symbol and timeframe in StockSharp.
2. Configure the parameters, especially `CandleType`, `LotSize`, and indicator periods.
3. Start the strategy; it will subscribe to candles, compute the indicators, and execute trades according to the described logic.
