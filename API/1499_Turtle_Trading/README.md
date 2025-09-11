# Turtle Trading Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Classic Turtle Trading system using Donchian breakouts and ATR-based risk management.

## Details

- **Entry Criteria**: breakout of Donchian channel upper/lower band
- **Long/Short**: both
- **Exit Criteria**: cross of shorter Donchian channel or trailing stop
- **Stops**: ATR-based initial and trailing stop
- **Default Values**:
  - `EntryLength` = 20
  - `ExitLength` = 10
  - `EntryLengthMode2` = 55
  - `ExitLengthMode2` = 20
  - `AtrPeriod` = 14
  - `RiskPerTrade` = 0.02
  - `InitialStopAtrMultiple` = 2
  - `PyramidAtrMultiple` = 0.5
  - `MaxUnits` = 4
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: DonchianChannels, ATR
  - Stops: ATR
  - Complexity: Advanced
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
