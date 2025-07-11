# Donchian CCI Strategy

This strategy uses Donchian CCI indicators to generate signals.
Long entry occurs when Price > Donchian Upper && CCI < -100 (breakout up with oversold conditions). Short entry occurs when Price < Donchian Lower && CCI > 100 (breakout down with overbought conditions).
It is suitable for traders seeking opportunities in mixed markets.

## Details
- **Entry Criteria**:
  - **Long**: Price > Donchian Upper && CCI < -100 (breakout up with oversold conditions)
  - **Short**: Price < Donchian Lower && CCI > 100 (breakout down with overbought conditions)
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit long position when price falls below middle band
  - **Short**: Exit short position when price rises above middle band
- **Stops**: Yes.
- **Default Values**:
  - `DonchianPeriod` = 20
  - `CciPeriod` = 20
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Mixed
  - Direction: Both
  - Indicators: Donchian CCI
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium
