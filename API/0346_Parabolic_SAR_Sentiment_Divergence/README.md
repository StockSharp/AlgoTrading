# Parabolic SAR Sentiment Divergence

The **Parabolic SAR Sentiment Divergence** strategy is built around Parabolic SAR Sentiment Divergence.

Signals trigger when Parabolic confirms divergence setups on intraday (5m) data. This makes the method suitable for active traders.

Stops rely on ATR multiples and factors like StartAf, MaxAf. Adjust these defaults to balance risk and reward.

## Details
- **Entry Criteria**: see implementation for indicator conditions.
- **Long/Short**: Both directions.
- **Exit Criteria**: opposite signal or stop logic.
- **Stops**: Yes, using indicator-based calculations.
- **Default Values**:
  - `StartAf = 0.02m`
  - `MaxAf = 0.2m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Parabolic, Divergence
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: Yes
  - Risk Level: Medium
