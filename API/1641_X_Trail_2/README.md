# X Trail 2
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades based on the crossover of two configurable moving averages calculated from a chosen price type.

## Details
- **Entry**: Buys when MA1 crosses above MA2 and this cross is confirmed by the previous two bars; sells when the opposite occurs.
- **Exit**: Opposite crossover.
- **Indicators**: Two moving averages with selectable type (simple, exponential, smoothed, weighted) and price source (close, open, high, low, median, typical, weighted).
- **Parameters**:
  - `Ma1Length` = 1
  - `Ma1Type` = MovingAverageTypeEnum.Simple
  - `Ma1PriceType` = AppliedPriceType.Median
  - `Ma2Length` = 14
  - `Ma2Type` = MovingAverageTypeEnum.Simple
  - `Ma2PriceType` = AppliedPriceType.Median
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
