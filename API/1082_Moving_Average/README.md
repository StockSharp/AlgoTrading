# Moving Average Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy enters a long position when a short moving average crosses above a long moving average of the selected price type. The position is closed when the short average crosses back below the long average.

## Details
- **Entry Criteria:** Short MA crosses above long MA.
- **Exit Criteria:** Short MA crosses below long MA.
- **Indicators:** SMA, EMA, DEMA, TEMA, WMA, VWMA.
- **Price Source:** Close, High, Open, Low, Typical, Center.
- **Stops:** None.
- **Default Values:**
  - `MaType` = EMA
  - `ShortLength` = 1
  - `LongLength` = 20
  - `PriceType` = Typical
  - `CandleType` = 1 minute
- **Filters:**
  - Category: Trend following
  - Direction: Long only
  - Indicators: Moving average
  - Stops: No
  - Complexity: Simple
  - Risk level: Medium
