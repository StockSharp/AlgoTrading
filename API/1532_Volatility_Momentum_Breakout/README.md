# Volatility Momentum Breakout Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Combines ATR-based breakout levels with EMA trend filter and RSI momentum to capture strong moves.

## Details

- **Entry Criteria**: price closes above/below ATR breakout levels with EMA and RSI confirmation
- **Long/Short**: Both
- **Exit Criteria**: ATR-based stop loss and 1:2 risk-reward take profit
- **Stops**: ATR stop
- **Default Values**:
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 1.5
  - `Lookback` = 20
  - `EmaPeriod` = 50
  - `RsiPeriod` = 14
  - `RsiLongThreshold` = 50
  - `RsiShortThreshold` = 50
  - `RiskReward` = 2
  - `AtrStopMultiplier` = 1
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: ATR, EMA, RSI, Highest, Lowest
  - Stops: ATR
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
