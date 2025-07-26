# RSI Donchian Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
The RSI Donchian strategy looks for momentum extremes that coincide with breakouts of the Donchian Channel. The relative strength index gauges overbought and oversold conditions while the channel defines recent price highs and lows.

A buy signal appears when the RSI dips below 30 and price breaks above the Donchian upper band. A short signal forms when the RSI rises above 70 and price falls through the lower band. Exits occur once price moves back to the Donchian middle line, signalling a return to balance.

This method works well for active traders who like to fade exhaustion moves but still trade with clear breakout levels. The stop-loss helps cap risk if momentum fails to revert quickly.

## Details
- **Entry Criteria**:
  - **Long**: RSI < 30 && Close > Donchian High
  - **Short**: RSI > 70 && Close < Donchian Low
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit when close < Donchian Middle
  - **Short**: Exit when close > Donchian Middle
- **Stops**: Yes, percentage stop-loss.
- **Default Values**:
  - `RsiPeriod` = 14
  - `DonchianPeriod` = 20
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filters**:
  - Category: Mixed
  - Direction: Both
  - Indicators: RSI, Donchian Channel
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium

Testing indicates an average annual return of about 82%. It performs best in the stocks market.
