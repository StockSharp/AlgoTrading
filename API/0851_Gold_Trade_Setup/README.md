# Gold Trade Setup Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy based on Kaufman Adaptive Moving Average and SuperTrend.
It sells when AMA is rising and SuperTrend switches to uptrend.
It buys when AMA is falling and SuperTrend switches to downtrend.

## Details

- **Entry Criteria**: AMA direction with SuperTrend flip.
- **Long/Short**: Both directions.
- **Exit Criteria**: Fixed target and stop levels.
- **Stops**: Yes.
- **Default Values**:
  - `AmaLength` = 14
  - `FastLength` = 2
  - `SlowLength` = 30
  - `AtrPeriod` = 10
  - `Factor` = 3.0
  - `TargetMultiplier` = 3.0
  - `RiskMultiplier` = 1.0
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: KAMA, SuperTrend
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
