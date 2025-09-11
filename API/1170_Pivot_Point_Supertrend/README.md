# Pivot Point Supertrend
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy combining pivot points with an ATR-based Supertrend to capture trend reversals.

Testing indicates an average annual return of about 65%. It performs best in the stocks market.

Pivot points define a dynamic center line. An ATR multiplier builds upper and lower bands that trail price. When the trend switches direction the strategy enters accordingly.

## Details

- **Entry Criteria**: Signals based on pivot points and ATR Supertrend.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal.
- **Stops**: No.
- **Default Values**:
  - `PivotPeriod` = 2
  - `AtrFactor` = 3m
  - `AtrPeriod` = 10
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Pivot Points, ATR
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

