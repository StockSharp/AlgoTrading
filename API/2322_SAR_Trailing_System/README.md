# SAR Trailing System Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that enters random long or short positions at fixed time intervals and manages exits using the Parabolic SAR indicator.
The Parabolic SAR value acts as a trailing stop: the position closes when price crosses the SAR level.

## Details

- **Entry Criteria**:
  - Every `TimerInterval`, if there is no open position and `UseRandomEntry` is enabled, a random long or short trade is opened.
- **Long/Short**: Both
- **Exit Criteria**: Price crossing the Parabolic SAR.
- **Stops**: Initial stop-loss in ticks with Parabolic SAR trailing exit.
- **Default Values**:
  - `TimerInterval` = 300 seconds
  - `StopLossTicks` = 10
  - `AccelerationStep` = 0.02
  - `AccelerationMax` = 0.2
  - `UseRandomEntry` = true
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filters**:
  - Category: Trend-following
  - Direction: Both
  - Indicators: Parabolic SAR
  - Stops: Yes
  - Complexity: Beginner
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
