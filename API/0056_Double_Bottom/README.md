# Double Bottom Pattern

This pattern-based strategy scans for two consecutive lows at roughly the same price separated by a set distance. After the second bottom forms, a bullish candle confirms the reversal.

When confirmation occurs, the system buys with a stop below the pattern lows. The setup aims to capture sharp rebounds from exhausted selling.

Exits rely on a predefined stop-loss or manual profit targets.

## Details

- **Entry Criteria**: Two bottoms form within `SimilarityPercent` after `Distance` bars.
- **Long/Short**: Long only.
- **Exit Criteria**: Price fails or stop-loss.
- **Stops**: Yes.
- **Default Values**:
  - `Distance` = 5
  - `SimilarityPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(15)
  - `StopLossPercent` = 1.0m
- **Filters**:
  - Category: Pattern
  - Direction: Long
  - Indicators: Price Action
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: Yes
  - Risk Level: Medium
