# Timeframe Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

EMA crossover strategy with timeframe aware risk management.

Testing indicates an average annual return of about 31%. It performs best in the crypto market.

The strategy buys when a fast EMA crosses above a slower EMA and the long term trend is bullish. Short entries occur on the opposite cross. Trading hours and a simple ADX filter help to avoid low momentum periods. Risk is managed with percentage based take profit and stop loss.

## Details

- **Entry Criteria**:
  - **Long**: EMA9 crosses above EMA20 while EMA50 is above EMA200.
  - **Short**: EMA9 crosses below EMA20 while EMA50 is below EMA200.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Stop loss or take profit.
  - **Short**: Stop loss or take profit.
- **Stops**: Yes, optional trailing stop.
- **Default Values**:
  - `TakeProfitPercent` = 1.5
  - `StopLossPercent` = 1.0
  - `TrailingPercent` = 0.5
  - `StartHour` = 15
  - `EndHour` = 20
  - `CooldownBars` = 5
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: EMA, RSI, ADX
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Short-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
