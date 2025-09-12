# Kaufman Trend Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The **Kaufman Trend Strategy** uses a Kalman filter to estimate price and momentum. Trend strength is derived from the filter's velocity component and normalized over a recent window. Entries occur when strong trend conditions align with price being above or below the filtered value. Stops are based on recent swings plus ATR and profits are taken in stages as momentum weakens.

## Details
- **Entry Criteria**: trend strength threshold with price above/below the filtered value.
- **Long/Short**: Both directions.
- **Exit Criteria**: staged take profits and trend weakening or stop hit.
- **Stops**: yes, swing low/high minus/plus ATR.
- **Default Values**:
  - `TakeProfit1Percent = 50`
  - `TakeProfit2Percent = 25`
  - `TakeProfit3Percent = 25`
  - `SwingLookback = 10`
  - `AtrPeriod = 14`
  - `TrendStrengthEntry = 60`
  - `TrendStrengthExit = 40`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Kalman
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday (15m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
