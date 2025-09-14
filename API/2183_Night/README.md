# Night Stochastic Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Night Stochastic strategy trades only during the quiet night session from **21:00** to **06:00**. It uses the %K line of the Stochastic Oscillator to detect oversold and overbought conditions.

When the oscillator drops below the oversold level a long position is opened. When it rises above the overbought level a short position is opened. Each trade is protected by fixed stop loss and take profit levels measured in price points.

## Details

- **Entry Criteria**:
  - **Long**: `%K < StochOversold` and time is between 21:00 and 06:00.
  - **Short**: `%K > StochOverbought` and time is between 21:00 and 06:00.
- **Long/Short**: Both directions.
- **Exit Criteria**: Position closed by predefined stop loss or take profit.
- **Stops**: Yes, uses fixed stop loss and take profit.
- **Default Values**:
  - `StopLossPoints` = 40
  - `TakeProfitPoints` = 20
  - `StochOversold` = 30
  - `StochOverbought` = 70
  - `CandleType` = 15 minute timeframe
- **Filters**:
  - Category: Indicator based
  - Direction: Both
  - Indicators: Stochastic Oscillator
  - Timeframe: Short term
  - Trading window: 21:00-06:00 server time
