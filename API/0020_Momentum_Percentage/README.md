# Momentum Percentage

Strategy based on price momentum percentage change

Momentum Percentage tracks percent change in price. Trades trigger when momentum exceeds positive or negative levels and exit on the counter signal or a volatility stop.

By measuring returns over a set lookback, the system adapts to different markets. The volatility stop ensures large adverse moves exit quickly.


## Details

- **Entry Criteria**: Signals based on MA, Momentum.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or stop.
- **Stops**: Yes.
- **Default Values**:
  - `MomentumPeriod` = 10
  - `ThresholdPercent` = 5m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: MA, Momentum
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
