# Long-Only Opening Range Breakout (ORB) with Pivot Points
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy buys when price breaks above the opening range high and the first resistance (R1) from daily pivot points sits above that high. A trailing stop follows pivot levels.

## Details

- **Entry Criteria**:
  - After the opening range, enter long on a breakout above the session high if R1 is higher.
- **Long/Short**: Long only
- **Exit Criteria**:
  - Trailing stop adjusted to pivot levels and daily close.
- **Stops**: Yes
- **Default Values**:
  - `RangeMinutes` = 15
  - `SessionStart` = 09:30
  - `MaxTradesPerDay` = 1
  - `StopLossPercent` = 3
  - `InitialSlType` = Percentage
- **Filters**:
  - Category: Breakout
  - Direction: Long
  - Indicators: Pivot Points
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
