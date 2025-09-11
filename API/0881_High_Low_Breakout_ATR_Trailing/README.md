# High-Low Breakout ATR Trailing Stop Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades breakouts of the first 30-minute session range. Once price crosses the initial high or low, a position opens with an ATR-based trailing stop. All positions close at a specified intraday time.

## Details
- **Entry Criteria**:
  - **Long**: Close crosses above the first 30-minute high
  - **Short**: Close crosses below the first 30-minute low
- **Long/Short**: Configurable (`Direction`).
- **Exit Criteria**:
  - ATR trailing stop or symmetric target
  - Close all positions at `ExitHour:ExitMinute`
- **Stops**: Yes, ATR-based.
- **Default Values**:
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 3.5m
  - `RiskPerTrade` = 2m
  - `AccountSize` = 10000m
  - `SessionStartHour` = 9
  - `SessionStartMinute` = 15
  - `ExitHour` = 15
  - `ExitMinute` = 15
  - `CandleType` = TimeSpan.FromMinutes(30)
- **Filters**:
  - Category: Breakout
  - Direction: Configurable
  - Indicators: ATR
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium
