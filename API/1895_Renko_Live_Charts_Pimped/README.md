# Renko Live Charts Pimped Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy builds renko bricks and trades on direction changes. It can optionally calculate the brick size from ATR values, allowing the renko structure to adapt to market volatility.

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
  - `CalculateBestBoxSize` = false.
  - `AtrPeriod` = 24.
  - `AtrCandleType` = 60m.
  - `UseAtrMa` = true.
  - `AtrMaPeriod` = 120.
- **Filters**:
  - Category: Trend Following
  - Direction: Both
  - Indicators: Renko, ATR
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Renko
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
