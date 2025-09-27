# Dual Momentum Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Rotates between a risky and a safe asset using dual momentum.
The strategy invests in the risky asset only when its momentum is positive and greater than the safe asset's momentum.

## Details

- **Entry Criteria**: Risky momentum > 0 and > safe momentum
- **Long/Short**: Long only
- **Exit Criteria**: Switch to safe asset when condition fails
- **Stops**: No
- **Default Values**:
  - `Period` = 12
  - `CandleType` = TimeSpan.FromDays(30).TimeFrame()
- **Filters**:
  - Category: Momentum
  - Direction: Long-only
  - Indicators: RateOfChange
  - Stops: No
  - Complexity: Basic
  - Timeframe: Monthly
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
