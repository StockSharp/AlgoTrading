# Breakouts With Timefilter Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Breakout strategy that enters on price crossing above recent highs or below recent lows within a specified trading session. An optional moving average filter confirms direction. Stop-loss can be based on ATR, candle extremes, or fixed points with a configurable risk-reward target.

## Details

- **Entry Criteria**:
  - **Long**: Close > highest high over `Length` and within time window; optionally Close > MA.
  - **Short**: Close < lowest low over `Length` and within time window; optionally Close < MA.
- **Long/Short**: Both
- **Stops**: ATR, candle-based, or fixed points with risk-reward target
- **Default Values**:
  - `Length` = 5
  - `MaLength` = 99
  - `UseMaFilter` = false
  - `UseTimeFilter` = true (14:30–15:00)
  - `SlType` = Atr
  - `SlLength` = 0
  - `AtrLength` = 14
  - `AtrMultiplier` = 0.5
  - `PointsStop` = 50
  - `RiskReward` = 3
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
