# Follow Line Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy tracks a follow line derived from Bollinger Bands breakouts with optional ATR offset. Entries occur when the line flips direction, optionally confirmed by a higher timeframe trend.

## Details

- **Entry Criteria**: Follow line changes direction after price breaks Bollinger Bands with optional higher timeframe confirmation.
- **Long/Short**: Both directions.
- **Exit Criteria**: Follow line or higher timeframe trend reverses.
- **Stops**: No.
- **Default Values**:
  - `AtrPeriod` = 5
  - `BbPeriod` = 21
  - `BbDeviation` = 1
  - `UseAtrFilter` = true
  - `UseTimeFilter` = false
  - `Session` = "0000-2400"
  - `UseHtfConfirmation` = false
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `HtfCandleType` = TimeSpan.FromHours(4)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Bollinger Bands, ATR
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
