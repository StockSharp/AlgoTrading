# Accumulation/Distribution Trend
[Русский](README_ru.md) | [中文](README_cn.md)
 
This strategy uses the Accumulation/Distribution (A/D) indicator to gauge buying and selling pressure. Rising A/D alongside price above the moving average signals accumulation, while falling A/D below the average indicates distribution.

Testing indicates an average annual return of about 187%. It performs best in the stocks market.

Trades are taken in the direction of the A/D trend relative to the moving average. A change in A/D direction acts as an exit signal.

Stops are optional but can help manage risk.

## Details

- **Entry Criteria**: A/D rising with price above MA or falling below MA.
- **Long/Short**: Both directions.
- **Exit Criteria**: A/D reverses or stop.
- **Stops**: Yes.
- **Default Values**:
  - `MAPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: A/D, MA
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

