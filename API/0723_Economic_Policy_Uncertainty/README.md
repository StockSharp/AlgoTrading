# Economic Policy Uncertainty
[Русский](README_ru.md) | [中文](README_cn.md)

The Economic Policy Uncertainty (EPU) strategy goes long when the two-period SMA of the EPU index crosses above a user-defined threshold. After entering a position the strategy waits a fixed number of bars before closing it.

This approach seeks to capture times when policy uncertainty rises above normal levels.

## Details

- **Entry Criteria**: SMA crosses above threshold.
- **Long/Short**: Long only.
- **Exit Criteria**: Exit after specified number of bars.
- **Stops**: No.
- **Default Values**:
  - `Threshold` = 187
  - `SmaLength` = 2
  - `ExitPeriods` = 10
  - `CandleType` = TimeSpan.FromDays(1)
- **Filters**:
  - Category: Breakout
  - Direction: Long
  - Indicators: SMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Daily
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
