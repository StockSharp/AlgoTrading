# Smart Factors Momentum Market
[Русский](README_ru.md) | [中文](README_zh.md)

The **Smart Factors Momentum Market** strategy blends multiple equity factors with a broad market trend filter. The system goes long the market only when both the momentum factor basket and the overall index show positive trends, and exits to cash otherwise.

## Details
- **Entry Criteria**: Composite factor momentum and market trend confirmation.
- **Long/Short**: Long only.
- **Exit Criteria**: Exit when factor momentum or market trend turns negative.
- **Stops**: No explicit stop.
- **Default Values**:
  - `CandleType = TimeSpan.FromDays(1).TimeFrame()`
- **Filters**:
  - Category: Momentum
  - Direction: Long
  - Indicators: Multiple
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Medium-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
