# ADX System
[Русский](README_ru.md) | [中文](README_cn.md)

The **ADX System** strategy trades using the Average Directional Index and its DI components. It opens a position when the ADX rises and one of the directional lines crosses above the ADX. Positions include fixed take-profit and stop-loss levels with a trailing stop to protect profit.

## Details

- **Entry Criteria**
  - ADX is rising (previous ADX below current).
  - For **long** trades: previous +DI below previous ADX and current +DI above current ADX.
  - For **short** trades: previous -DI below previous ADX and current -DI above current ADX.
- **Exit Criteria**
  - Opposite signal on ADX and DI lines.
  - Price reaches the trailing stop level.
  - Price hits the fixed take-profit or stop-loss.
- **Long/Short**: Both directions.
- **Stops**: Fixed stop-loss, take-profit, and trailing stop in absolute price units.
- **Default Values**:
  - `AdxPeriod` = 14
  - `TakeProfit` = 15
  - `StopLoss` = 100
  - `TrailingStop` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend-following
  - Direction: Both
  - Indicators: ADX, +DI, -DI
  - Stops: Yes
  - Complexity: Beginner
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

