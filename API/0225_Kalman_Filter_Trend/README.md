# Kalman Filter Trend Strategy

This trend-following method uses a Kalman filter to smooth price fluctuations and estimate the underlying direction. The filter dynamically adapts to market noise, offering a refined view of trend strength compared to standard moving averages.

A long position is opened when the closing price rises above the Kalman filter estimate. Conversely, a short position is taken when the close drops below the filter value. Because the filter updates on every bar, trades flip whenever price crosses the line, providing continuous participation in trending markets.

Traders who prefer systematic approaches may find the Kalman filter useful for reducing whipsaws. A protective stop based on ATR keeps risk limited in case the trend rapidly reverses.

## Details
- **Entry Criteria**:
  - **Long**: Close > Kalman Filter
  - **Short**: Close < Kalman Filter
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit on close < Kalman Filter
  - **Short**: Exit on close > Kalman Filter
- **Stops**: Yes, ATR-based stop-loss.
- **Default Values**:
  - `ProcessNoise` = 0.01m
  - `MeasurementNoise` = 0.1m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Kalman Filter
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium
