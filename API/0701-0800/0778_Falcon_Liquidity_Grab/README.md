# Falcon Liquidity Grab Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy trades liquidity grabs during major market sessions using a simple moving average to define trend. It enters when price wicks beyond recent swing levels and reverses with the trend. Each trade uses fixed stop loss and take profit measured in ticks.

## Details

- **Entry Conditions**:
  - **Long**: `Low < lowest(swing period)` && `Close > SMA` && `session filter`
  - **Short**: `High > highest(swing period)` && `Close < SMA` && `session filter`
- **Exit Conditions**: fixed stop loss and take profit.
- **Type**: Reversal
- **Indicators**: SMA, Highest, Lowest
- **Timeframe**: 15 minutes (default)
- **Stops**: `StopLossPoints` ticks, `TakeProfitMultiplier`× stop distance

**Exact rule and defaults:** `SwingPeriod=20`, `TrendLength=50`, `StopLossPoints=50`, `TakeProfitMultiplier=2`, `UseSession=true`, `SessionStart=07:00`, `SessionEnd=21:00` (UTC), `CandleType=15 minutes`. At a completed candle, test the wick against the **preceding** N highs/lows (exclude the current candle). Enter long if its low is strictly below the preceding minimum and its close strictly above SMA; enter short if its high is strictly above the preceding maximum and its close strictly below SMA. The SMA includes the completed signal candle. Only flat positions may enter; the session interval applies only to entries and is UTC inclusive at start/exclusive at end, including overnight windows. Native market protection runs on Level1 bid/ask updates between candles: stop distance is `StopLossPoints × Security.PriceStep`; target distance is `TakeProfitMultiplier × stop distance`. There is no bar-close or opposing-signal exit.
