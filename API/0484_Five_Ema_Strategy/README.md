# 5 EMA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The 5 EMA strategy marks a candle that closes entirely below or above the 5-period EMA. If price breaks the signal candle's extreme within three bars and outside the block window, the strategy enters in the breakout direction. Targets are based on a user-defined reward-to-risk ratio and trades can be forcibly closed at a specific time.

## Details

- **Entry Criteria**:
  - Candle close and high below EMA → mark for long; buy when price crosses above signal high within 3 bars.
  - Candle close and low above EMA → mark for short; sell when price crosses below signal low within 3 bars.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Stop at signal candle opposite extreme.
  - Target at `TargetRR` × risk.
  - Optional exit at custom time (`ExitHour`, `ExitMinute`).
- **Stops**: Yes.
- **Default Values**:
  - `EmaLength` = 5
  - `TargetRR` = 3.0
  - `ExitHour` = 15, `ExitMinute` = 30
  - `BlockStartHour` = 15, `BlockStartMinute` = 0
  - `BlockEndHour` = 15, `BlockEndMinute` = 30
- **Filters**:
  - Category: Breakout
  - Direction: Long/Short
  - Indicators: EMA
  - Stops: Yes
  - Complexity: Low
  - Timeframe: 5-minute
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
