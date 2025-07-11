# MA Deviation

Strategy that trades when price deviates significantly from its moving average

MA Deviation enters when price deviates a set percentage from its moving average, anticipating a return to the mean. The position is exited when price converges back toward the average.

Deviation thresholds can be widened or narrowed depending on volatility. Using ATR for position sizing keeps risk consistent across markets.


## Details

- **Entry Criteria**: Signals based on MA, ATR.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or stop.
- **Stops**: Yes.
- **Default Values**:
  - `MAPeriod` = 20
  - `DeviationPercent` = 5m
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: MA, ATR
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
