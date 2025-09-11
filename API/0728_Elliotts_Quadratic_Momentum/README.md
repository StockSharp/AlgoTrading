# Elliott's Quadratic Momentum
[Русский](README_ru.md) | [中文](README_zh.md)

The **Elliott's Quadratic Momentum** strategy combines multiple SuperTrend indicators to capture Elliott-wave inspired momentum.

The strategy enters long when all four SuperTrend lines signal an uptrend and enters short when all signal a downtrend. Positions are closed when any SuperTrend reverses direction.

## Details
- **Entry Criteria**: All SuperTrend indicators align in the same direction.
- **Long/Short**: Both directions.
- **Exit Criteria**: Any SuperTrend flips against the position.
- **Stops**: No explicit stops.
- **Default Values**:
  - `AtrLength1 = 7`
  - `Multiplier1 = 4.0m`
  - `AtrLength2 = 14`
  - `Multiplier2 = 3.618m`
  - `AtrLength3 = 21`
  - `Multiplier3 = 3.5m`
  - `AtrLength4 = 28`
  - `Multiplier4 = 3.382m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: SuperTrend
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
