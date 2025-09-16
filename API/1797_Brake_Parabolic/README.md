# Brake Parabolic Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy implements the Brake Parabolic indicator that projects a parabolic barrier above or below price. When the barrier is broken, the trend flips and a new position is opened in the direction of the breakout. The algorithm trails the extreme price with a curved line defined by parameters **A**, **B**, and **Shift**.

Testing indicates an average annual return of about 48%. It performs best in trending markets on higher timeframes.

The system waits for the barrier to switch sides. A bullish flip closes any short and opens a new long position. A bearish flip closes any long and opens a short. While in a trend, opposite positions are closed when the indicator confirms the direction.

## Details

- **Entry Criteria**:
  - **Long**: Barrier switches from above price to below price.
  - **Short**: Barrier switches from below price to above price.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or indicator confirms opposing trend.
- **Stops**: No fixed stops; exits rely on barrier reversal.
- **Default Values**:
  - `A` = 1.5
  - `B` = 1.0
  - `BeginShift` = 10
  - `CandleType` = 4-hour timeframe
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Custom
  - Stops: No
  - Complexity: Medium
  - Timeframe: Swing
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

