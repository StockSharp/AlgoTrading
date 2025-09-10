# Bollinger Heikin Ashi Entry Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy using Bollinger Bands on Heikin Ashi candles. Buys after two consecutive bearish Heikin Ashi candles touching the lower band followed by a bullish candle above it. Sells in reverse.

After entering, a first target equal to the risk is taken and the stop is trailed using the previous candle's extremes.

## Details

- **Entry Criteria**:
  - Long: two bearish HA candles touching lower band then bullish above it
  - Short: two bullish HA candles touching upper band then bearish below it
- **Long/Short**: Both
- **Exit Criteria**:
  - Long: first target at 1R then trailing stop at previous lows
  - Short: first target at 1R then trailing stop at previous highs
- **Stops**: Previous candle low/high
- **Default Values**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Reversal
  - Direction: Both
  - Indicators: Bollinger Bands, Heikin Ashi
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
