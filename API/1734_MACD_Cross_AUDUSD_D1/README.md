# MACD Cross AUDUSD D1
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy trades AUDUSD on the daily timeframe using MACD line crossovers.

The strategy opens a long position when the MACD main line crosses above the signal line and a short position when it crosses below. Trading is allowed only between 06:00 and 14:00 server time, and only one position can be open at a time. Each trade sets a stop loss of 40 pips and a take profit three times larger by default.

## Details

- **Entry Criteria**: MACD main line crossing the signal line.
- **Long/Short**: Both directions.
- **Exit Criteria**: Stop loss or take profit.
- **Stops**: Yes.
- **Default Values**:
  - `Volume` = 0.1
  - `StopLossPips` = 40
  - `RewardRatio` = 3
  - `CandleType` = TimeSpan.FromDays(1)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: MACD
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Daily
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
