# Renko Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy enters long when a bullish renko brick follows a bearish one and enters short on the opposite change.

## Details

- **Entry Criteria**:
  - **Long**: bullish renko brick after a bearish brick.
  - **Short**: bearish renko brick after a bullish brick.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Reverse signal.
- **Stops**: No.
- **Default Values**:
  - `BoxSize` = 10m.
  - `Volume` = 1m.
  - `CandleType` = Renko.
- **Filters**:
  - Category: Trend Following
  - Direction: Both
  - Indicators: Renko
  - Stops: No
  - Complexity: Basic
  - Timeframe: Renko
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
