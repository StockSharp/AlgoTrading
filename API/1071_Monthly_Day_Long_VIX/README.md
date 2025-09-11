# Monthly Day Long VIX Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Opens a long position on a specified day of each month when the VIX index is below a threshold. The trade is closed after a fixed number of bars or when stop loss or take profit is reached.

## Details

- **Entry Criteria**:
  - `EntryDay` occurs and VIX < `VixThreshold`.
- **Long/Short**: Long only.
- **Exit Criteria**: After `HoldDuration` bars or by protection.
- **Stops**: Stop loss, take profit.
- **Default Values**:
  - `EntryDay` = 27
  - `HoldDuration` = 4
  - `VixThreshold` = 20
  - `StopLossPercent` = 2
  - `TakeProfitPercent` = 5
  - `CandleType` = TimeSpan.FromDays(1)
- **Filters**:
  - Category: Seasonality
  - Direction: Long
  - Indicators: VIX
  - Stops: Stop loss, Take profit
  - Complexity: Basic
  - Timeframe: Daily
  - Seasonality: Yes
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
