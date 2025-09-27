# Triangle Breakout Strategy with TP SL and EMA Filter
[Русский](README_ru.md) | [中文](README_cn.md)

Detects triangle patterns from pivot highs and lows. Enters long on breakout above the triangle, optionally requiring price above EMA20 and EMA50, and uses percentage-based take-profit and stop-loss.

## Details

- **Entry Criteria**: close above triangle upper line with optional EMA20/EMA50 filter
- **Long/Short**: Long
- **Exit Criteria**: percentage take-profit or stop-loss
- **Stops**: Yes
- **Default Values**:
  - `PivotLength` = 5
  - `TakeProfitPercent` = 3
  - `StopLossPercent` = 1.5
  - `UseEmaFilter` = true
  - `EmaFast` = 20
  - `EmaSlow` = 50
  - `CandleType` = 1 hour
- **Filters**:
  - Category: Pattern
  - Direction: Long
  - Indicators: EMA, Pivot
  - Stops: Yes
  - Complexity: Advanced
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
