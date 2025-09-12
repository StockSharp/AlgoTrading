# Horizontal Ray Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Draws horizontal rays at SMA cross points and trades in the cross direction.

## Details

- **Entry Criteria**: `SMA20` crossing `SMA50` upward for long, downward for short.
- **Long/Short**: Both.
- **Exit Criteria**: Opposite cross.
- **Stops**: No.
- **Default Values**:
  - `FastLength` = 20
  - `SlowLength` = 50
  - `CandleType` = 5 minutes
- **Filters**:
  - Category: Drawing
  - Direction: Both
  - Indicators: SMA
  - Stops: No
  - Complexity: Beginner
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
