# Divergence + EMA + RSI Close Buy Only

## Overview

This strategy ports the MetaTrader "Divergence + ema + rsi close buy only" expert advisor to StockSharp's high-level API. It acts on **5-minute candles** while consulting **hourly** and **daily** data to confirm trend alignment and oversold conditions. Orders are long-only. Entries require a bullish MACD histogram divergence that is confirmed by an hourly stochastic crossover inside a tight oversold band and by a rising daily EMA structure. Exits rely on a fixed RSI overshoot combined with optional stop-loss and take-profit protection managed by the framework.

## Trading Logic

1. **Higher-timeframe trend filter**
   - Daily EMA(9) must be above EMA(20) to ensure a prevailing uptrend.
   - The latest 5-minute close has to remain below the daily EMA(9) so that long entries are attempted from pullbacks.

2. **Hourly stochastic confirmation**
   - The most recent completed hourly stochastic %K value must lie between the `StochasticLowerBound` (default 0) and `StochasticUpperBound` (default 40).
   - %K must have crossed above %D on the last hourly bar (current %K > %D while the previous %K ≤ previous %D).

3. **MACD divergence trigger (5-minute)**
   - The MACD histogram (MACD line minus signal line) must improve by at least `MacdThreshold` while the 5-minute close sets a lower low compared with the previous candle. This approximates the bullish divergence used by the original EA.

4. **Entry execution**
   - When all filters align and no long position is open, the strategy sends a market buy. If an unexpected short position exists, the requested volume is increased to neutralize it before flipping long.

5. **Exit rules**
   - A protective RSI exit closes the long when the 5-minute RSI crosses above `RsiExitLevel` (default 77).
   - `StartProtection` activates both stop-loss and take-profit levels converted from pips into price distances whenever the corresponding parameters are positive.

6. **Order management**
   - All active orders are cancelled prior to sending a new market buy order to avoid duplicated fills.
   - Volume defaults to the `TradeVolume` parameter and can be adjusted for optimization.

## Parameters

| Name | Description | Default |
|------|-------------|---------|
| `CandleType` | Primary timeframe for MACD, RSI, and execution. | 5-minute candles |
| `HourTimeFrame` | Hourly timeframe used by the stochastic filter. | 1 hour |
| `DayTimeFrame` | Daily timeframe for EMA trend confirmation. | 1 day |
| `MacdFastPeriod` / `MacdSlowPeriod` / `MacdSignalPeriod` | MACD structure on the primary timeframe. | 6 / 13 / 5 |
| `MacdThreshold` | Minimum MACD histogram increase to accept a divergence. | 0.0003 |
| `DailyFastPeriod` / `DailySlowPeriod` | Daily EMA periods. | 9 / 20 |
| `StochasticKPeriod` / `StochasticDPeriod` / `StochasticSlowing` | Hourly stochastic configuration. | 30 / 5 / 9 |
| `StochasticUpperBound` / `StochasticLowerBound` | Accepted hourly %K range. | 40 / 0 |
| `RsiPeriod` | RSI length on the primary timeframe. | 7 |
| `RsiExitLevel` | RSI value that forces long exits. | 77 |
| `TradeVolume` | Base order size for buys. | 0.01 |
| `StopLossPips` | Stop-loss distance in pips (0 disables). | 100 |
| `TakeProfitPips` | Take-profit distance in pips (0 disables). | 200 |

## Notes

- The strategy subscribes to three data streams: the configured primary timeframe, an hourly series, and a daily series. Each stream drives its own indicator set via `Bind`/`BindEx` to keep the implementation concise and event-driven.
- Indicator values are only processed on finished candles to mirror the original EA's shift parameters.
- The MACD divergence detection uses the previous bar's close and histogram value as a simple yet robust approximation of the builder-generated logic from the source MQL file.
- Stop-loss and take-profit are handled by `StartProtection` to remain synchronized with broker fills and support backtesting or live trading without manual order replication.
