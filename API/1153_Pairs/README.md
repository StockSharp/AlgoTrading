# Pairs Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This pair trading strategy buys when the reference asset closes above its open while the current symbol forms a down bar. The position closes when price breaks above the previous candle's high.

## Details

- **Entry Criteria**: Reference asset up & current down bar.
- **Long/Short**: Long only.
- **Exit Criteria**: Close above previous high.
- **Stops**: No.
- **Default Values**:
  - `CandleType` = 1 minute
- **Filters**:
  - Category: Pair trading
  - Direction: Long-only
  - Indicators: Price action
  - Stops: No
  - Complexity: Beginner
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
