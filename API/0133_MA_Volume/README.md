# MA Volume Strategy

MA Volume combines a moving average trend filter with volume surges to time entries.
Rising volume alongside price above the average signals strong accumulation; falling volume below the average indicates distribution.

The strategy trades in the direction of the moving average when volume expands, exiting once volume dries up or the average reverses.

A percent stop protects against sudden shifts in trend.

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
  - Indicators: Moving Average, Volume
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
