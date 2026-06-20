# 3-Bar Low Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

The 3-Bar Low strategy buys when the closing price falls below the previous three-bar lowest close and exits when price closes above the previous seven-bar highest close. An optional EMA filter can require the price to stay above a long-term average before entries are allowed.

## Details

- **Entry Criteria**:
  - Closing price is below the previous three-bar lowest close.
  - Optional: closing price is above the EMA when the filter is enabled.
- **Long/Short**: Long only.
- **Exit Criteria**:
  - Closing price is above the previous seven-bar highest close.
- **Stops**: None.
- **Default Values**:
  - `MaPeriod` = 200
  - `LowestLength` = 3
  - `HighestLength` = 7
  - `UseEmaFilter` = false
- **Filters**:
  - Category: Breakout
  - Direction: Long
  - Indicators: EMA, Highest/Lowest
  - Stops: No
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
