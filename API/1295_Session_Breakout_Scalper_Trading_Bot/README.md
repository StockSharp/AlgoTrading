# Session Breakout Scalper Trading Bot Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Session Breakout Scalper trades breakouts of the price range formed during a predefined session.

## Details

- **Entry Criteria**: price breaks above session high or below session low
- **Long/Short**: Both
- **Exit Criteria**: take profit or stop loss
- **Stops**: ATR or fixed
- **Default Values**:
  - `SessionStart` = 01:00
  - `SessionEnd` = 02:00
  - `TakeProfit` = 100
  - `StopLoss` = 50
  - `UseAtrStop` = true
  - `AtrLength` = 14
  - `AtrMultiplier` = 2
  - `CandleType` = time frame 1 minute
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: ATR
  - Stops: ATR
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
