# RSI Option Open Interest

The **RSI Option Open Interest** strategy is built around RSI Option Open Interest.

Signals trigger when Option confirms trend changes on intraday (5m) data. This makes the method suitable for active traders.

Stops rely on ATR multiples and factors like RsiPeriod, CandleType. Adjust these defaults to balance risk and reward.

## Details
- **Entry Criteria**: see implementation for indicator conditions.
- **Long/Short**: Both directions.
- **Exit Criteria**: opposite signal or stop logic.
- **Stops**: Yes, using indicator-based calculations.
- **Default Values**:
  - `RsiPeriod = 14`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
  - `OiPeriod = 20`
  - `OiDeviationFactor = 2m`
  - `StopLoss = 2m`
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Option, Open, Interest
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
