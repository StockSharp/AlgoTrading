# S&P 100 Option Expiration Week Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy buys at the beginning of the option expiration week (the week containing the third Friday of the month) and closes the position on that third Friday.

## Details

- **Entry Criteria**:
  - **Long**: open a long position on Monday of the option expiration week.
- **Long/Short**: Long only.
- **Exit Criteria**:
  - close the long position on the third Friday of the month.
- **Stops**: No.
- **Default Values**:
  - `CandleType` = TimeSpan.FromDays(1).TimeFrame().
- **Filters**:
  - Category: Seasonality
  - Direction: Long
  - Indicators: None
  - Stops: No
  - Complexity: Basic
  - Timeframe: Daily
  - Seasonality: Yes
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
