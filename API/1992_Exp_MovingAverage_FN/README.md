# Exp Moving Average FN Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades based on slope reversals of an exponential moving average (EMA). It enters long when the EMA turns upward after a decline and enters short when the EMA turns downward after a rise. Optional stop-loss and take-profit levels are defined in absolute price units.

## Details

- **Entry Criteria**:
  - **Long**: EMA slope changes from falling to rising.
  - **Short**: EMA slope changes from rising to falling.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Opposite slope reversal.
  - Stop-loss or take-profit hit.
- **Stops**: Yes, using absolute price distances.
- **Default Values**:
  - `EMA Length` = 12
  - `Stop Loss` = 1000
  - `Take Profit` = 2000
  - `Candle Type` = 4-hour timeframe
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Single (EMA)
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Medium-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Moderate
