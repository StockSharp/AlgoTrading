# Buy And Hold Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy enters a single long position at the specified start date and holds it until the end date, implementing a simple buy and hold approach.

## Details

- **Entry Criteria**:
  - When a candle time is on or after the start date the strategy buys once.
- **Long/Short**: Long only.
- **Exit Criteria**:
  - When a candle time reaches or exceeds the end date the position is closed.
- **Stops**: None.
- **Default Values**:
  - Start date = 2018-01-01.
  - End date = 2069-12-31.
- **Filters**:
  - Category: Buy and Hold.
  - Direction: Long.
  - Indicators: None.
  - Stops: No.
  - Complexity: Low.
  - Timeframe: Any.
  - Seasonality: No.
  - Neural networks: No.
  - Divergence: No.
  - Risk level: High.
