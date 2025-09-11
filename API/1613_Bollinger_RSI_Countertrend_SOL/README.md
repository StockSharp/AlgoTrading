# Bollinger RSI Countertrend SOL Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Countertrend system for SOL that buys when price crosses above the lower Bollinger Band with low RSI and sells when price crosses below the upper band with high RSI. Weekdays only.

## Details

- **Entry Criteria**:
  - **Long**: Price crosses above lower band and `RSI` < `Long RSI` on weekdays.
  - **Short**: Price crosses below upper band and `RSI` > `Short RSI` on weekdays.
- **Long/Short**: Both directions.
- **Exit Criteria**:
  - Long: price crosses above upper band or stop loss under recent lows.
  - Short: price crosses above middle band or reaches profit target.
- **Stops**: Long stop below recent lows.
- **Default Values**:
  - `Bollinger Period` = 20
  - `Bollinger Width` = 2
  - `RSI Length` = 14
  - `Long RSI` = 25
  - `Short RSI` = 79
  - `Short Profit %` = 3.5
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: Multiple
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: Yes (Weekdays)
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
