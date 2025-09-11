# Dow Theory Trend Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Dow Theory Trend Strategy uses pivot highs and lows to determine trend direction. The strategy enters long when both higher highs and higher lows appear, and enters short when both lower highs and lower lows form.

## Details

- **Entry Criteria**: Higher highs and higher lows for long; lower highs and lower lows for short.
- **Long/Short**: Both directions.
- **Exit Criteria**: Reverse signal.
- **Stops**: None.
- **Default Values**:
  - `PivotLookback` = 10
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Price action
  - Stops: No
  - Complexity: Basic
  - Timeframe: Any
  - Seasonality: No
  - Neural Networks: No
  - Divergence: Yes
  - Risk Level: Medium
