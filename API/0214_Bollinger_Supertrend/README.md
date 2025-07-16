# Bollinger Supertrend Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
This strategy blends Bollinger Bands with the Supertrend indicator to pinpoint entries during strong directional moves. Bollinger Bands gauge volatility expansion while the Supertrend line tracks the overall trend and acts as a trailing stop.

A long trade triggers when price closes above the upper Bollinger Band and remains above the Supertrend line, confirming momentum and trend alignment. A short trade occurs when price closes below the lower band while staying under the Supertrend level. Trades are exited once price crosses back through the Supertrend, indicating momentum has faded.

Because the system waits for breakouts beyond normal volatility, it suits traders looking to capture sustained runs rather than quick reversals. The Supertrend stop dynamically adjusts to market swings, helping manage risk without manual intervention.

## Details
- **Entry Criteria**:
  - **Long**: Close > upper Bollinger Band && Close > Supertrend
  - **Short**: Close < lower Bollinger Band && Close < Supertrend
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit when price crosses below Supertrend
  - **Short**: Exit when price crosses above Supertrend
- **Stops**: Yes, via Supertrend trailing stop.
- **Default Values**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3.0m
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Bollinger Bands, Supertrend
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium
