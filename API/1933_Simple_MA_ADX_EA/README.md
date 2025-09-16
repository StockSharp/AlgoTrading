# Simple MA ADX EA
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy combining an EMA with the Average Directional Index to confirm trend strength.

It buys when the EMA is rising, the previous close is above the EMA, ADX exceeds a threshold and +DI is greater than -DI. It sells when the opposite conditions appear. Stop-loss and take-profit levels manage risk.

## Details

- **Entry Criteria**: EMA direction, price vs EMA, ADX, +DI/-DI.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or protection orders.
- **Stops**: Yes.
- **Default Values**:
  - `AdxPeriod` = 8
  - `MaPeriod` = 8
  - `AdxThreshold` = 22m
  - `StopLoss` = 30m
  - `TakeProfit` = 100m
  - `Volume` = 0.1m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: EMA, ADX
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (1m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

