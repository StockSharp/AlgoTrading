# Liquidity Breakout Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades breakouts from a recent price range defined by pivot highs and lows. A position is opened when price closes beyond the previous range extremes. Optional stop loss can use a SuperTrend line or fixed percentage.

## Details

- **Entry Criteria**:
  - `Close > previous range high` → long
  - `Close < previous range low` → short
- **Long/Short**: Configurable (Long, Short, Both).
- **Exit Criteria**: Opposite breakout or stop loss.
- **Stops**: SuperTrend or fixed percentage.
- **Default Values**:
  - `PivotLength` = 12
  - `StopLoss` = SuperTrend
  - `FixedPercentage` = 0.1
  - `SuperTrendPeriod` = 10
  - `SuperTrendMultiplier` = 3
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Highest, Lowest, SuperTrend
  - Stops: Optional
  - Complexity: Low
  - Timeframe: 1h
