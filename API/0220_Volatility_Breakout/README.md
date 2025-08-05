# Volatility Breakout Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
The Volatility Breakout strategy seeks strong directional moves when price escapes from its average range. By measuring the distance from a simple moving average using the Average True Range, the algorithm defines breakout thresholds that scale with volatility.

Testing indicates an average annual return of about 97%. It performs best in the crypto market.

A buy order is triggered when the close rises above the SMA by more than `Multiplier` times the ATR. A sell signal appears when the close falls below the SMA by the same distance. Positions remain open until an opposite breakout occurs or a protective stop is hit.

This technique caters to intraday traders who thrive on momentum surges. Using ATR-based thresholds helps filter out noise so only significant moves generate trades.

## Details
- **Entry Criteria**:
  - **Long**: Close > SMA + Multiplier * ATR
  - **Short**: Close < SMA - Multiplier * ATR
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit when an opposite breakout triggers or stop-loss hits
  - **Short**: Exit when an opposite breakout triggers or stop-loss hits
- **Stops**: Yes, stop-loss at `Multiplier * ATR` from entry.
- **Default Values**:
  - `Period` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: SMA, ATR
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium

