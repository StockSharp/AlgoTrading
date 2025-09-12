# Timezone Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Demonstration strategy showing how to format candle times for different time zones.

Testing indicates an average annual return of about 0%. It performs best in any market.

The strategy subscribes to candles and logs their close times converted to a user-selected timezone. It can be used as a template for time-based studies or scheduling.

## Details

- **Entry Criteria**: None
- **Long/Short**: None
- **Exit Criteria**: None
- **Stops**: No
- **Default Values**:
  - `CandleType` = 5m
  - `Timezone` = Utc
- **Filters**:
  - Category: Utility
  - Direction: None
  - Indicators: None
  - Stops: No
  - Complexity: Simple
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
