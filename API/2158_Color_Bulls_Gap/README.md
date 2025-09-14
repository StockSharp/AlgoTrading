# Color Bulls Gap Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that recreates the ColorBullsGap indicator by comparing smoothed gaps between the high price and averages of open and close.
Enters long when a bullish color two bars ago turns neutral or bearish on the last bar, closing any short positions.
Enters short when a bearish color two bars ago turns neutral or bullish on the last bar, closing any long positions.

## Details

- **Entry Criteria**:
  - Long: `PrevColor == 0 && LastColor > 0`
  - Short: `PrevColor == 2 && LastColor < 2`
- **Long/Short**: Both
- **Exit Criteria**: Opposite signal
- **Stops**: No
- **Default Values**:
  - `Length1` = 12
  - `Length2` = 5
  - `CandleType` = TimeSpan.FromHours(8).TimeFrame()
- **Filters**:
  - Category: Indicator
  - Direction: Both
  - Indicators: SMA
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
