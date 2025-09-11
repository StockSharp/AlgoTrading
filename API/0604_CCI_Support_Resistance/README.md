# CCI Support Resistance Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses CCI pivots to build dynamic support and resistance levels. A trend filter based on EMA cross or slope is applied before trading breakouts of these levels.

## Details

- **Entry Criteria**:
  - Long: price closes above the CCI-based support after touching it and the trend is bullish.
  - Short: price closes below the CCI-based resistance after touching it and the trend is bearish.
- **Long/Short**: Both.
- **Exit Criteria**:
  - ATR-based stop loss and take profit.
- **Stops**: Yes, ATR-based.
- **Default Values**:
  - `CciLength` = 50
  - `LeftPivot` = 50
  - `RightPivot` = 50
  - `Buffer` = 10
  - `TrendMatter` = true
  - `TrendType` = Cross
  - `SlowMaLength` = 100
  - `FastMaLength` = 50
  - `SlopeLength` = 5
  - `Ksl` = 1.1
  - `Ktp` = 2.2
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: CCI, EMA, ATR
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
