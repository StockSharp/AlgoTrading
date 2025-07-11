# ADX Trend

Strategy based on Average Directional Index (ADX) trend
The ADX Trend strategy gauges market strength using the ADX indicator. When ADX is above a threshold and price is on the correct side of its moving average, the system trades in that direction. Positions close once ADX weakens or the opposite setup appears.

By waiting for a solid ADX reading, the approach only trades when momentum is firmly established. Stops typically use an ATR multiple so risk adjusts with volatility.


## Details

- **Entry Criteria**: Signals based on MA, ADX, ATR.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or stop.
- **Stops**: Yes.
- **Default Values**:
  - `AdxPeriod` = 14
  - `MaPeriod` = 50
  - `AtrMultiplier` = 2m
  - `AdxExitThreshold` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: MA, ADX, ATR
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
