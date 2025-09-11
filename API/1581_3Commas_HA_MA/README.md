# 3Commas HA & MA
[Русский](README_ru.md) | [中文](README_cn.md)

Uses Heikin Ashi candles and a pair of exponential moving averages. A long trade occurs when a bearish HA candle is followed by a bullish one while the fast MA is above the slow MA. Shorts follow the opposite setup. Positions are closed when price crosses the slow MA or hits the swing stop.

## Details
- **Entry Criteria**: Heikin Ashi reversal with MA confirmation.
- **Long/Short**: Both directions.
- **Exit Criteria**: Price crosses slow MA or stop.
- **Stops**: Swing high/low.
- **Default Values**:
  - `MaFast` = 9
  - `MaSlow` = 18
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Heikin Ashi, EMA
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
