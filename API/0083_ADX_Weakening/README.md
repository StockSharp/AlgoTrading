# ADX Weakening Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
The Average Directional Index measures trend strength. When ADX begins to decline it often signals that the current move is losing momentum. This system trades against that weakening trend when price is on the opposite side of a simple moving average.

For each bar the strategy computes ADX and an MA. If ADX decreases compared to the prior value and price is above the MA, a long entry is placed. If ADX falls while price is below the MA, it goes short. A fixed stop-loss protects the position.

Because the approach anticipates a slowdown rather than a full reversal, trades usually hold only until ADX starts to rise again or the stop is hit.

## Details

- **Entry Criteria**: ADX lower than previous value and price relative to MA.
- **Long/Short**: Both.
- **Exit Criteria**: Stop-loss.
- **Stops**: Yes, percentage based.
- **Default Values**:
  - `AdxPeriod` = 14
  - `MaPeriod` = 20
  - `StopLoss` = 2%
  - `CandleType` = 15 minute
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: ADX, MA
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
