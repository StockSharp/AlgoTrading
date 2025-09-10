# BTC Seasonality Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy opens a position based on predefined day-of-week and hour rules using Eastern Standard Time (EST). The user chooses the entry day and hour, the exit day and hour, and whether to trade long or short. The position is opened at the specified entry moment and closed at the specified exit moment.

## Details

- **Entry Criteria**:
  - Current EST day equals `EntryDay` and current hour equals `EntryHour`.
- **Long/Short**: Configurable.
- **Exit Criteria**:
  - Current EST day equals `ExitDay` and current hour equals `ExitHour`.
- **Stops**: None.
- **Default Values**:
  - `EntryDay` = Saturday
  - `ExitDay` = Monday
  - `EntryHour` = 10
  - `ExitHour` = 10
  - `IsLong` = true
- **Filters**:
  - Category: Seasonality
  - Direction: Configurable
  - Indicators: None
  - Stops: No
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: Yes
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
