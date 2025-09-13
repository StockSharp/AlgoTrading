# Bollinger Breakout
[Русский](README_ru.md) | [中文](README_cn.md)

Converted from the original MQL strategy. Trades Bollinger Band breakouts confirmed by EMA, MACD, and RSI. The strategy enters only once per volatility expansion and trails the stop along the middle band while using a fixed take profit in pips.

## Details

- **Entry Criteria**:
  - Long: band width above `BreakoutFactor`, MACD > 0, RSI > 50, EMA above middle band, previous close above previous upper band
  - Short: band width above `BreakoutFactor`, MACD < 0, RSI < 50, EMA below middle band, previous close below previous lower band
- **Long/Short**: Both
- **Exit Criteria**:
  - Long: price touches trailing middle-band stop or hits take profit
  - Short: price touches trailing middle-band stop or hits take profit
- **Stops**: Stop level is the current middle Bollinger Band, updated every candle
- **Take Profit**: Fixed distance specified in pips
- **Default Values**:
  - `BollingerLength` = 18
  - `BollingerDeviation` = 2m
  - `BreakoutFactor` = 0.0015m
  - `TakeProfitPips` = 100
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Bollinger Bands, EMA, MACD, RSI
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
