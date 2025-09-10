# ATR Based Trendlines
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that builds ATR based trendlines from pivot points and trades their breakouts.

## Details

- **Entry Criteria**: Breakout of ATR-based trendlines.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite breakout.
- **Stops**: No.
- **Default Values**:
  - `LookbackLength` = 30
  - `AtrPercent` = 1.0
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: ATR, Price Action
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (1m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
