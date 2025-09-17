# Dynamic Averaging Strategy

## Overview
Dynamic Averaging is a direct port of the MetaTrader 5 expert advisor "Dynamic averaging.mq5" (id 23319). The strategy combines a fast stochastic oscillator with a volatility filter based on standard deviation. Trades are only allowed while the market volatility remains below its rolling average, forcing entries to occur during consolidations where stochastic reversals are more reliable.

## Parameters
- **TradeVolume** – order size for every new entry. It is automatically doubled after a losing sequence and reset after a profitable one.
- **MinimumProfit** – floating profit (in account currency) that closes all open positions once exceeded.
- **SlidingWindowDays** – number of calendar days used to average the standard deviation values and build the volatility baseline.
- **StochasticKPeriod** – number of bars for the %K calculation.
- **StochasticDPeriod** – smoothing length for the %D line.
- **StochasticSlowPeriod** – final slowing period for the stochastic oscillator.
- **StdDevPeriod** – lookback period for the standard deviation indicator.
- **CandleType** – source candles for calculations (defaults to 15-minute time frame).

## Trading Rules
1. The strategy operates on finished candles only. At the close of each bar the stochastic and volatility filters are updated via `SubscribeCandles().BindEx`.
2. Calculate the market volatility using `StandardDeviation(StdDevPeriod)` and compare it with the average volatility computed by `SimpleMovingAverage` over the last `SlidingWindowDays` worth of bars.
3. If the current standard deviation is above the rolling average, the bar is skipped.
4. When volatility is muted:
   - Enter **long** if %K is below 25 and the slope of the previous two %K values is positive (last value minus the value two bars ago).
   - Enter **short** if %K is above 75 and the slope of the previous two %K values is negative.
5. Positions are reversed by sending enough volume to flatten the opposite side plus the new `TradeVolume` exposure.
6. Whenever the floating PnL of the open position exceeds `MinimumProfit`, the strategy immediately exits the market.

## Position Sizing and Recovery
- The initial order size equals `TradeVolume`.
- After the position is closed, the realized PnL change is inspected.
  - A **loss** doubles the next trade size (`martingale` step) to replicate the original EA behaviour.
  - A **profit or breakeven** resets the size back to the base `TradeVolume`.

## Implementation Details
- Candles, stochastic and standard deviation values are processed through the high-level API with `BindEx`, avoiding manual buffer management.
- The sliding volatility window converts calendar days into bar counts by using the candle time frame if available.
- Floating profit control relies on the current candle close and `PositionAvgPrice`, matching the MQL implementation that sums open-position profit only.
- All code comments are written in English; no Python version is provided per task requirements.
