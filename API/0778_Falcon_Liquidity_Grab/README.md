# Falcon Liquidity Grab Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

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
