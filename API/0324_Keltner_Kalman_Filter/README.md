# Keltner Kalman Filter

The **Keltner Kalman Filter** strategy is built around combining Keltner Channels with a Kalman Filter to identify trends and trade opportunities.

Signals trigger when Keltner confirms filtered entries on intraday (15m) data. This makes the method suitable for active traders.

Stops rely on ATR multiples and factors like EmaPeriod, AtrPeriod. Adjust these defaults to balance risk and reward.

## Details
- **Entry Criteria**: see implementation for indicator conditions.
- **Long/Short**: Both directions.
- **Exit Criteria**: opposite signal or stop logic.
- **Stops**: Yes, using indicator-based calculations.
- **Default Values**:
  - `EmaPeriod = 20`
  - `AtrPeriod = 14`
  - `AtrMultiplier = 2.0m`
  - `KalmanProcessNoise = 0.01m`
  - `KalmanMeasurementNoise = 0.1m`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Keltner
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday (15m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
