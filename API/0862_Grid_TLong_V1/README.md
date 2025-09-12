# Grid TLong V1 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Grid-based strategy that continuously maintains a position. It re-enters positions when profit or loss reaches a fixed percent step.

## Details

- **Entry Criteria**: Always stay in the market; restart positions at grid steps.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or re-entry after reaching the percent step.
- **Stops**: No.
- **Default Values**:
  - `Percent` = 1
  - `UseLimitOrders` = false
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Grid
  - Direction: Both
  - Indicators: None
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
