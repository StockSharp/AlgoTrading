# VWMA Cross

The Volume Weighted Moving Average (VWMA) emphasizes price levels with higher trading volume. This strategy trades crossovers between price and the VWMA.

A close above VWMA after being below it generates a long entry, while a drop below VWMA prompts a short trade. Positions exit when price crosses back in the opposite direction.

Using a volume-weighted average reduces noise from low-volume periods.

## Rules

- **Entry Criteria**: Price crosses VWMA from below or above.
- **Long/Short**: Both directions.
- **Exit Criteria**: Reverse crossover or stop.
- **Stops**: Yes.
- **Default Values**:
  - `VWMAPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: VWMA
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
