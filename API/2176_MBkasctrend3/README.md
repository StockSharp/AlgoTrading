# MBKAsctrend3 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The MBKAsctrend3 strategy uses three Williams %R oscillators with different periods. Their weighted combination defines the market trend. A long position opens when the weighted value crosses above an upper threshold and the long-term oscillator is also high. A short position opens when the values fall below their lower thresholds. Positions are protected by configurable stop-loss and take-profit levels expressed in points.

## Details
- **Entry Criteria**:
  - **Long**: Weighted WPR > 67+Swing and long WPR > 50-AverageSwing.
  - **Short**: Weighted WPR < 33-Swing and long WPR < 50+AverageSwing.
- **Long/Short**: Both.
- **Exit Criteria**: Opposite signal or protective levels.
- **Stops**: Absolute stop loss and take profit.
- **Filters**: None.

## Parameters
- `WprLength1`, `WprLength2`, `WprLength3` – periods of the three Williams %R indicators.
- `Swing` – shift of upper/lower thresholds.
- `AverageSwing` – additional shift based on long term oscillator.
- `Weight1`, `Weight2`, `Weight3` – weights for each indicator.
- `StopLoss`, `TakeProfit` – protection levels in points.
- `CandleType` – timeframe of candles, default 4 hours.
