# Smart Money Pivot Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades breakouts of pivot highs and lows. A long position opens when price breaks above the latest pivot high, while a short position opens when price drops below the latest pivot low. Each trade uses its own stop-loss and take-profit percentages.

## Details

- **Entry Criteria**: Break above pivot high or below pivot low.
- **Long/Short**: Both directions.
- **Exit Criteria**: Stop-loss or take-profit.
- **Stops**: Yes.
- **Default Values**:
  - `EnableLongStrategy` = true
  - `LongStopLossPercent` = 1m
  - `LongTakeProfitPercent` = 1.5m
  - `EnableShortStrategy` = true
  - `ShortStopLossPercent` = 1m
  - `ShortTakeProfitPercent` = 1.5m
  - `Period` = 20
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Price Action
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (1m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
