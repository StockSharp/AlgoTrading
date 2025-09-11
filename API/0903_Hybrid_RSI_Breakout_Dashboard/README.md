# Hybrid RSI Breakout Dashboard
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy combines RSI mean reversion with breakout entries filtered by ADX and a 200 EMA.

The system buys when the market is ranging and RSI drops below `RsiBuy` in bullish EMA trend. It sells short when RSI rises above `RsiSell` in bearish trend. In trending regime, it enters breakouts above/below recent closes and trails the position using ATR.

Includes a start date filter and simple dashboard variables for last trade type and direction.

## Details

- **Entry Criteria**: RSI signals in ranging regime with EMA bias, or breakouts above/below previous `BreakoutLength` closes when ADX > `AdxThreshold`.
- **Long/Short**: Both.
- **Exit Criteria**: RSI trades exit on `RsiExit`. Breakout trades use ATR trailing stop.
- **Stops**: ATR trailing stop for breakout trades.
- **Default Values**:
  - `AdxLength` = 14
  - `AdxThreshold` = 20m
  - `EmaLength` = 200
  - `RsiLength` = 14
  - `RsiBuy` = 40m
  - `RsiSell` = 60m
  - `RsiExit` = 50m
  - `BreakoutLength` = 20
  - `AtrLength` = 14
  - `AtrMultiplier` = 2m
  - `StartDate` = 2017-01-01
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend, Mean Reversion
  - Direction: Both
  - Indicators: ADX, EMA, RSI, ATR, Highest/Lowest
  - Stops: Trailing
  - Complexity: Medium
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
