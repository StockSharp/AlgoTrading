# CCI Breakout

Strategy based on CCI (Commodity Channel Index) breakout

CCI Breakout uses the Commodity Channel Index to spot powerful moves. Surges beyond positive or negative CCI thresholds generate entries. Exits happen when CCI retreats toward zero or an opposite signal forms.

Because CCI measures deviation from a moving average, extreme readings imply unsustainable prices. This system waits for those extremes and then attempts to profit from the follow-through.


## Details

- **Entry Criteria**: Signals based on CCI, Momentum.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or stop.
- **Stops**: Yes.
- **Default Values**:
  - `CciPeriod` = 20
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: CCI, Momentum
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
