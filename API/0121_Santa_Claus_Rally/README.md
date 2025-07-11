# Santa Claus Rally Strategy

The Santa Claus Rally describes the tendency for stocks to rise in the final week of December through the first two trading days of January.
Holiday optimism and year-end positioning can fuel this short burst of strength.

The strategy buys at the start of the period and exits after the second trading day of the new year, aiming to capture the seasonal lift.

Stops are kept small to avoid large losses if the market fails to rally during the window.

## Details

- **Entry Criteria**: calendar effect triggers
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filters**:
  - Category: Seasonality
  - Direction: Both
  - Indicators: Seasonality
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: Yes
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
