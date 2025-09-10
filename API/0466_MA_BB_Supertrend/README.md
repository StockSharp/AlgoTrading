# MA + BB + SuperTrend Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy combines a moving average crossover with SuperTrend confirmation
and uses Bollinger Bands for exits. A long position is opened when the signal
MA crosses above the basis MA and price is above the SuperTrend line. Shorts
are opened on the opposite cross under a bearish SuperTrend. Positions are
closed either when price touches the far Bollinger Band or when price crosses
the SuperTrend in the opposite direction.

## Details

- **Entry Criteria**:
  - Signal MA crosses basis MA in direction of SuperTrend.
- **Long/Short**: Both directions.
- **Exit Criteria**:
  - Touch of opposite Bollinger Band or SuperTrend flip.
- **Stops**: SuperTrend acts as trailing stop.
- **Default Values**:
  - MA signal length = 89, MA ratio = 1.08.
  - BB length = 30, BB width = 3.
  - SuperTrend period = 20, SuperTrend factor = 4.
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: MA, Bollinger Bands, SuperTrend
  - Stops: SuperTrend
  - Complexity: Moderate
  - Timeframe: Short/medium
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
