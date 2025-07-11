# MA Stochastic Strategy

MA Stochastic uses a moving average trend filter with stochastic oscillator pullbacks.
When price trends above the average and the stochastic dips into oversold, the system prepares to buy the next upturn.

Short trades mirror this logic for downtrends, selling rallies when stochastic reaches overbought.

Fixed percent stops help avoid large losses if the trend suddenly reverses.

## Details

- **Entry Criteria**: indicator signal
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Moving Average, Stochastic
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
