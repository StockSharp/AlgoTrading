# Triple EMA + QQE Trend Following Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Trend-following strategy combining two TEMA lines with a QQE filter.
It opens long positions when price is above both TEMA lines and QQE gives a bullish signal.
Short positions are opened on opposite conditions.
A trailing stop in points protects open trades.

## Details

- **Entry Criteria**: TEMA alignment with QQE crossover.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or trailing stop.
- **Stops**: Yes.
- **Default Values**:
  - `RsiPeriod` = 14
  - `RsiSmoothing` = 5
  - `QqeFactor` = 4.238m
  - `Tema1Length` = 20
  - `Tema2Length` = 40
  - `StopLossPips` = 120
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: EMA, QQE
  - Stops: Trailing
  - Complexity: Intermediate
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
