# Grid Bot Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The grid bot divides a predefined price range into equal levels and trades the oscillations between them. When price drifts toward the lower half of the grid the strategy accumulates long positions, selling them as price returns to the upper half. This approach thrives in sideways markets with clear bounds.

No directional bias is assumed; the bot simply reacts to proximity to grid lines.

## Details

- **Entry Criteria**:
  - **Long**: price touches a level in the lower half while no long position
  - **Short**: price touches a level in the upper half while no short position
- **Long/Short**: Both sides
- **Exit Criteria**:
  - Opposite entry signal closes existing position
- **Stops**: None
- **Default Values**:
  - `UpperLimit` = 48000
  - `LowerLimit` = 45000
  - `GridCount` = 10
- **Filters**:
  - Category: Range trading
  - Direction: Both
  - Indicators: Price levels
  - Stops: No
  - Complexity: Low
  - Timeframe: Short-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
