# TFM
[Русский](README_ru.md) | [中文](README_cn.md)

Timeframe-multiplier breakout strategy. Uses a higher timeframe formed by multiplying the base timeframe. Long when price breaks above the previous high and optional short or exit when price falls below the previous low.

## Details
- **Entry Criteria**: Price crossing levels from multiplied timeframe.
- **Long/Short**: Long with optional short.
- **Exit Criteria**: Cross of opposite level or optional reversal.
- **Stops**: No.
- **Default Values**:
  - `CandleTime` = TimeSpan.FromMinutes(1)
  - `Multiplier` = 2
  - `AllowShort` = false
- **Filters**:
  - Category: Breakout
  - Direction: Both (if shorts enabled)
  - Indicators: High/Low
  - Stops: No
  - Complexity: Easy
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
