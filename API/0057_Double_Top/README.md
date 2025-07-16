# Double Top Pattern
[Русский](README_ru.md) | [中文](README_cn.md)
 
Double Top identifies two peaks separated by a number of bars with similar prices. After the second peak forms, a bearish candle confirms the reversal.

The strategy sells short upon confirmation with a stop above the pattern highs, aiming to profit from a trend change after buyers are exhausted.

Positions are closed via stop-loss or discretionary targets.

## Details

- **Entry Criteria**: Two tops within `SimilarityPercent` after `Distance` bars.
- **Long/Short**: Short only.
- **Exit Criteria**: Price rallies or stop-loss.
- **Stops**: Yes.
- **Default Values**:
  - `Distance` = 5
  - `SimilarityPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(15)
  - `StopLossPercent` = 1.0m
- **Filters**:
  - Category: Pattern
  - Direction: Short
  - Indicators: Price Action
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: Yes
  - Risk Level: Medium
