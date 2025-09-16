# MACFibo Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy implements the MACFibo trading system. It waits for a crossover between the 5-period EMA and the 20-period SMA. After the cross, the algorithm measures the swing from the close of the cross bar (point A) to the most recent extreme (point B) and builds Fibonacci expansion levels. Positions are opened at market price with take profit and stop loss derived from these levels. An optional exit closes losing trades when the fast EMA crosses the mid SMA in the opposite direction.

## Details

- **Entry Conditions:**
  - **Long:** 5 EMA crosses above 20 SMA. Point B is the lowest low since the downward move started.
  - **Short:** 5 EMA crosses below 20 SMA. Point B is the highest high since the upward move started.
- **Exit Conditions:**
  - Take profit at the 161.8% Fibonacci level or the minimum take profit distance.
  - Stop loss at the 38.2% Fibonacci level or the maximum stop loss distance.
  - Optional close if 5 EMA crosses 8 SMA against the position and the trade is losing.
- **Filters:**
  - Trades only between configured start and end hours.
  - Trading on Monday or Friday can be disabled.
- **Parameters:**
  - `FastLength` – fast EMA length.
  - `MidLength` – middle SMA length for protective exit.
  - `SlowLength` – slow SMA length for trend detection.
  - `MinTakeProfit` – minimum take profit in price units.
  - `MaxStopLoss` – maximum stop loss in price units.
  - `StartHour` / `EndHour` – allowed trading time window.
  - `FridayTrade` / `MondayTrade` – enable trading on these days.
  - `CloseAtFastMid` – close losing trades on fast-mid cross.
  - `CandleType` – candle type for calculations.
