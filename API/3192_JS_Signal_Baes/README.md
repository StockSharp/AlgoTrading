# JS Signal Baes Strategy

## Overview
This strategy is a StockSharp port of the MetaTrader expert advisor "JS Signal Baes". It evaluates six different timeframes simultaneously (M1, M5, M15, M30, H1, H4 by default) and waits until all monitored indicators agree on the same market direction before opening a position. Signals can be inverted through the **Reverse** parameter for users who want to trade counter to the detected trend.

## Indicators and Confirmations
The following indicators are calculated on each of the six timeframes:

- **Two Moving Averages** using the selected smoothing method (simple, exponential, smoothed or linear weighted).
- **MACD (Moving Average Convergence Divergence)** using configurable fast, slow and signal lengths.
- **RSI (Relative Strength Index)** with a dedicated period parameter.
- **CCI (Commodity Channel Index)** with its own lookback length.
- **Stochastic Oscillator** defined by K, D and smoothing periods.

A timeframe is considered **bullish** when:

1. Fast MA > Slow MA.
2. MACD main line > MACD signal line.
3. RSI > 50.
4. CCI > 0.
5. Stochastic %K > 40.

A timeframe is considered **bearish** when:

1. Fast MA < Slow MA.
2. MACD main line < MACD signal line.
3. RSI < 50.
4. CCI < 0.
5. Stochastic %K < 60.

## Trading Rules
A new netted position is opened only when the primary timeframe (default M1) closes and **all six timeframes** are simultaneously bullish or bearish:

- **Long entry:** every timeframe is bullish. If *Reverse* is enabled the signal becomes a short entry instead.
- **Short entry:** every timeframe is bearish. If *Reverse* is enabled the signal becomes a long entry instead.

Positions are not pyramided. The strategy waits until the existing position is closed externally before acting on a new signal. There are no automatic exits beyond the opposite signal logic from the original expert advisor.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| CciPeriod | 13 | Lookback length for the Commodity Channel Index. |
| FastMaPeriod | 5 | Length of the fast moving average. |
| SlowMaPeriod | 9 | Length of the slow moving average. |
| MaMethod | LinearWeighted | Moving average smoothing type applied to both averages. |
| MacdFastPeriod | 8 | Fast EMA length used by MACD. |
| MacdSlowPeriod | 17 | Slow EMA length used by MACD. |
| MacdSignalPeriod | 9 | Signal line length used by MACD. |
| StochasticKPeriod | 5 | K period for the stochastic oscillator. |
| StochasticDPeriod | 3 | D period for the stochastic oscillator. |
| StochasticSmoothing | 3 | Smoothing factor for the stochastic oscillator. |
| RsiPeriod | 9 | RSI lookback length. |
| ReverseSignals | false | Invert the direction of every trading signal. |
| TimeFrame1..6 | M1, M5, M15, M30, H1, H4 | Candle series assigned to each timeframe. |

## Notes
- The default parameters replicate the configuration embedded in the MetaTrader version.
- Money management, stop-loss, take-profit and trailing logic from the original code are not reproduced; use portfolio-level risk controls if required.
- Ensure that historical data are available for every selected timeframe so the indicators can warm up before trading.
