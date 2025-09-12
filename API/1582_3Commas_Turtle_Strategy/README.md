# 3Commas Turtle Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Simplified Turtle-style breakout system using Donchian channels. Buys on breakouts above the fast channel when price is also above the slow channel, and sells on breakdowns below the fast channel with confirmation from the slow channel. Exits occur when price crosses the exit channel in the opposite direction.

## Details
- **Entry Criteria**: Breakout of fast channel with slow channel confirmation.
- **Long/Short**: Both directions.
- **Exit Criteria**: Price crosses exit channel.
- **Stops**: Channel-based.
- **Default Values**:
  - `PeriodFast` = 20
  - `PeriodSlow` = 20
  - `PeriodExit` = 10
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Donchian channels
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
