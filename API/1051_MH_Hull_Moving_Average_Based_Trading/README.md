# MH Hull Moving Average Based Trading
[Русский](README_ru.md) | [中文](README_cn.md)

Hull Moving Average based breakout strategy.

The strategy compares the open price with dynamic levels derived from the Hull Moving Average. It enters long when price breaks above the upper level and short when it falls below the lower level. Existing positions are closed on opposite breakouts.

## Details

- **Entry Criteria**: Price relation to Hull Moving Average levels.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite breakout.
- **Stops**: No.
- **Default Values**:
  - `HullPeriod` = 210
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: MA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
