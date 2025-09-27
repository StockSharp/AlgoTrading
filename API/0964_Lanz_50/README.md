# LANZ Strategy 5.0
[Русский](README_ru.md) | [中文](README_cn.md)

The LANZ Strategy 5.0 trades in the direction of a 200-period EMA and requires three consecutive candles of the same color. It limits trades by daily count, New York time window, and minimum distance between entries.

## Details

- **Entry Criteria**:
  - Price above EMA and three bullish candles for long entries.
  - Price below EMA and three bearish candles for short entries (optional).
- **Long/Short**: Long by default.
- **Exit Criteria**:
  - Fixed stop loss or take profit.
  - Manual close at configured time.
- **Stops**:
  - Stop loss = 40 pips.
  - Take profit = 120 pips.
- **Default Values**:
  - `EmaPeriod` = 200
  - `MaxTrades` = 99
  - `MinDistancePips` = 25
  - `StopLossPips` = 40
  - `TakeProfitPips` = 120
  - `StartHour` = 19
  - `EndHour` = 15
- **Filters**:
  - Category: Trend
  - Direction: Long
  - Indicators: EMA
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
