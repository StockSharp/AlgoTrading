# Bollinger K-Means Cluster The **Bollinger K-Means Cluster** strategy is built around Bollinger K-Means Cluster.

Signals trigger when Bollinger confirms trend changes on intraday (5m) data. This makes the method suitable for active traders.

Stops rely on ATR multiples and factors like BollingerLength, BollingerDeviation. Adjust these defaults to balance risk and reward.

## Details
- **Entry Criteria**: see implementation for indicator conditions.
- **Long/Short**: Both directions.
- **Exit Criteria**: opposite signal or stop logic.
- **Stops**: Yes, using indicator-based calculations.
- **Default Values**:
  - `BollingerLength = 20`
  - `BollingerDeviation = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
  - `KMeansHistoryLength = 50`
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
