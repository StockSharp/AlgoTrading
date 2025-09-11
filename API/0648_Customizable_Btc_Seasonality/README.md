# Customizable BTC Seasonality Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy exploits intraday seasonality in Bitcoin by entering and exiting at user-defined UTC hours.
A long position is opened at the entry hour and closed at the exit hour.

## Details

- **Entry Criteria**: time equals user-defined entry hour
- **Long/Short**: Long only
- **Exit Criteria**: time equals user-defined exit hour
- **Stops**: No
- **Default Values**:
  - `CandleType` = 1 minute
  - `EntryHour` = 21
  - `ExitHour` = 23
- **Filters**:
  - Category: Seasonality
  - Direction: Long
  - Indicators: None
  - Stops: No
  - Complexity: Beginner
  - Timeframe: Intraday
  - Seasonality: Yes
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
