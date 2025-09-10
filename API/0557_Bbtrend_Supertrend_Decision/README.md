# BBTrend SuperTrend Decision Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy derives the **BBTrend** value from two Bollinger Bands with different lengths and
feeds it into a SuperTrend calculation. The resulting SuperTrend direction decides whether to
open long or short positions. Optional percentage based take‑profit and stop‑loss protections
can be enabled.

## Details

- **Entry Criteria**:
  - Long: SuperTrend direction is up.
  - Short: SuperTrend direction is down.
- **Long/Short**: Both, configurable.
- **Exit Criteria**:
  - Opposite SuperTrend direction.
- **Stops**: Optional percentage TP/SL.
- **Default Values**:
  - Short BB length = 20, Long BB length = 50, StdDev = 2.
  - SuperTrend length = 10, factor = 7.
  - Take Profit = 30%, Stop Loss = 20%.
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Bollinger Bands, SuperTrend
  - Stops: Optional TP/SL
  - Complexity: Moderate
  - Timeframe: Short
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
