# 2692 MACD Stochastic Strategy

## Overview
This strategy is a StockSharp port of the MetaTrader 5 system "MACD Stochastic". It combines a classic MACD crossover with an optional stochastic confirmation filter and trades only during three configurable intraday sessions. Each position uses pip-based risk controls with optional trailing stop logic that can move the stop toward break-even once the trade has reached a specified profit.

## Indicators
- **MACD (Moving Average Convergence Divergence)** – generates the primary trend reversal signals by tracking the crossover between the fast and slow exponential moving averages and their signal line.
- **Stochastic Oscillator** – optional filter that confirms MACD signals by checking that the %K and %D lines have recently crossed in the same direction as the trade.

## Trading Logic
### Long Entries
1. The MACD main line crosses above the signal line and both lines are below zero, indicating a potential bullish reversal.
2. The most recent position was opened on a previous bar (only one entry per bar is allowed).
3. The current time (local time of the instrument) falls inside one of the configured trading sessions.
4. If the stochastic filter is enabled, the current %K value must be above %D and the value from *StochasticBarsToCheck* bars ago must show the opposite relationship (%K below %D), confirming a fresh bullish crossover.

### Short Entries
1. The MACD main line crosses below the signal line and both lines are above zero, signaling a bearish reversal.
2. The strategy has no open position and did not already open a trade on the current bar.
3. The current time is inside at least one active session window.
4. When the stochastic filter is active, the current %K must be below %D and the value from *StochasticBarsToCheck* bars ago must be above %D, confirming a bearish crossover.

### Position Management
- **Stop-Loss / Take-Profit** – initial levels are calculated in pips using the instrument price step. The implementation automatically adjusts for 3- and 5-digit quotes by multiplying the price step by 10 to approximate a standard pip.
- **Trailing Stop** – once the position has earned at least *WhenSetNoLossStopPips* of profit, the stop can trail toward the market:
  - Long positions require an initial stop to be present. The stop is incremented by *TrailingStopPips* whenever it remains at least *TrailingStepPips + TrailingStopPips* away from the current close and stays above the break-even buffer defined by *NoLossStopPips*.
  - Short positions move the stop downward under similar constraints. If no initial stop exists, the algorithm can place a break-even stop at *NoLossStopPips* once price has advanced far enough.
- **Take-Profit / Stop Hits** – if a candle high or low touches the stored exit levels, the position is closed at market and the internal state resets.

## Parameters
- **MacdFastPeriod, MacdSlowPeriod, MacdSignalPeriod** – MACD configuration.
- **UseStochastic** – enables the stochastic confirmation filter.
- **StochasticBarsToCheck, StochasticLength, StochasticKPeriod, StochasticDPeriod** – stochastic oscillator settings.
- **Volume** – trade size in lots.
- **StopLossPips, TakeProfitPips** – pip distances for initial exits.
- **TrailingStopPips, TrailingStepPips** – trailing stop configuration.
- **NoLossStopPips, WhenSetNoLossStopPips** – break-even and activation thresholds for the trailing logic.
- **MaxPositions** – retained for compatibility; StockSharp works with net positions, so the strategy keeps only one open position at a time.
- **Session1/2/3 Start-End** – intraday windows when trading is allowed. Set both start and end to `00:00` to disable a window.
- **CandleType** – candle series used for signal generation.

## Additional Notes
- Entries are processed on completed candles only. The strategy will not open more than one position per candle, mirroring the original EA behaviour.
- Pip-based distances depend on the instrument price step. Ensure the symbol metadata provides a valid `PriceStep`.
- The stochastic filter stores a small rolling history to evaluate past values without using low-level indicator access, complying with the high-level API best practices.
