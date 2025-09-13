# ARD Order Management Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy using the DeMarker indicator crossing a 0.5 threshold to open positions.

When DeMarker drops below the threshold after being above, the strategy buys. When DeMarker rises above the threshold after being below, it sells. Exit occurs on the opposite signal. No stop-loss or take-profit is used.

## Details

- **Entry Criteria**:
  - Long: `DeMarker crosses below Threshold`
  - Short: `DeMarker crosses above Threshold`
- **Long/Short**: Both
- **Exit Criteria**: Opposite signal
- **Stops**: No
- **Default Values**:
  - `DeMarkerPeriod` = 2
  - `Threshold` = 0.5
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filters**:
  - Category: Indicator
  - Direction: Both
  - Indicators: DeMarker
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
