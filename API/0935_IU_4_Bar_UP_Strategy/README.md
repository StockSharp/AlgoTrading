# IU 4 Bar UP Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

IU 4 Bar UP Strategy is a long-only approach that buys after four consecutive bullish candles when price is above the SuperTrend indicator.

## Details
- **Data**: Price candles.
- **Entry Criteria**:
  - **Long**: Four consecutive bullish candles and close above SuperTrend.
- **Exit Criteria**: Close below SuperTrend.
- **Stops**: None.
- **Default Values**:
  - `SupertrendLength` = 14
  - `SupertrendMultiplier` = 1
- **Filters**:
  - Category: Trend following
  - Direction: Long
  - Indicators: SuperTrend
  - Complexity: Low
  - Risk level: Medium
