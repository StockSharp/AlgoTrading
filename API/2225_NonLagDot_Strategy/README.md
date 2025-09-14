# NonLagDot Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy inspired by the NonLagDot indicator. The indicator approximates price trend using a smooth moving average and color-coded dots.
The strategy opens a long position when the indicator turns upward and a short position when it turns downward.
Previous opposite positions are closed before opening a new one.

## Details

- **Entry Criteria**:
  - Long: Indicator turns from down to up (moving average slope becomes positive)
  - Short: Indicator turns from up to down (moving average slope becomes negative)
- **Long/Short**: Both
- **Exit Criteria**: Opposite signal
- **Stops**: Optional stop-loss percentage
- **Default Values**:
  - `Length` = 10
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
  - `StopLossPercent` = 1m
- **Filters**:
  - Category: Trend-following
  - Direction: Both
  - Indicators: SMA slope approximation of NonLagDot
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
