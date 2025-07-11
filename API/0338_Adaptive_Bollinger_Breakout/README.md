# Adaptive Bollinger Breakout

The **Adaptive Bollinger Breakout** strategy is built around that trades based on breakouts of Bollinger Bands with adaptively adjusted parameters.

Signals trigger when Bollinger confirms breakout opportunities on intraday (5m) data. This makes the method suitable for active traders.

Stops rely on ATR multiples and factors like MinBollingerPeriod, MaxBollingerPeriod. Adjust these defaults to balance risk and reward.

## Details
- **Entry Criteria**: see implementation for indicator conditions.
- **Long/Short**: Both directions.
- **Exit Criteria**: opposite signal or stop logic.
- **Stops**: Yes, using indicator-based calculations.
- **Default Values**:
  - `MinBollingerPeriod = 10`
  - `MaxBollingerPeriod = 30`
  - `MinBollingerDeviation = 1.5m`
  - `MaxBollingerDeviation = 2.5m`
  - `AtrPeriod = 14`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Bollinger
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
