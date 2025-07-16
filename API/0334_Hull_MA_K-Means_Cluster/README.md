# Hull MA K-Means Cluster
[Русский](README_ru.md) | [中文](README_zh.md)
 
The **Hull MA K-Means Cluster** strategy is built around that trades based on Hull Moving Average direction with K-Means clustering for market state detection.

Signals trigger when its indicators confirms trend changes on intraday (5m) data. This makes the method suitable for active traders.

Stops rely on ATR multiples and factors like HullPeriod, ClusterDataLength. Adjust these defaults to balance risk and reward.

## Details
- **Entry Criteria**: see implementation for indicator conditions.
- **Long/Short**: Both directions.
- **Exit Criteria**: opposite signal or stop logic.
- **Stops**: Yes, using indicator-based calculations.
- **Default Values**:
  - `HullPeriod = 9`
  - `ClusterDataLength = 50`
  - `RsiPeriod = 14`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: multiple indicators
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
