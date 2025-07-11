# ZScore

Strategy based on Z-Score indicator for mean reversion trading

ZScore measures price deviation from a moving average. Extreme high or low z-scores suggest overextension and prompt trades in the opposite direction. The trade ends when the z-score normalizes.

Z-Score is a flexible filter because it can be scaled to any time series. Using a volatility-adjusted exit helps the system adapt to changing market conditions.


## Details

- **Entry Criteria**: Signals based on MA, ZScore.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or stop.
- **Stops**: Yes.
- **Default Values**:
  - `ZScoreEntryThreshold` = 2.0m
  - `ZScoreExitThreshold` = 0.0m
  - `MAPeriod` = 20
  - `StdDevPeriod` = 20
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: MA, ZScore
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
