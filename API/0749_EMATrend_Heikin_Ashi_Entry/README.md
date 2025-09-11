# EMA Trend Heikin Ashi Entry Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy using Bollinger Bands on Heikin Ashi candles with a higher timeframe EMA trend filter. Buys after consecutive bearish Heikin Ashi candles touching the lower band followed by a bullish candle above it when the higher timeframe fast EMA is above the slow EMA. Sells in reverse.

After entering, a first target equal to the risk is taken and the stop is trailed using the previous candle's extremes.

## Details

- **Entry Criteria**:
  - Long: at least two bearish HA candles touching lower band, then bullish above it with higher timeframe fast EMA above slow EMA
  - Short: at least two bullish HA candles touching upper band, then bearish below it with higher timeframe fast EMA below slow EMA
- **Long/Short**: Both
- **Exit Criteria**:
  - Long: first target at 1R then trailing stop at previous lows
  - Short: first target at 1R then trailing stop at previous highs
- **Stops**: Previous candle low/high
- **Default Values**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2m
  - `FastEmaPeriod` = 9
  - `SlowEmaPeriod` = 21
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `HigherTimeframe` = TimeSpan.FromMinutes(180).TimeFrame()
- **Filters**:
  - Category: Pullback
  - Direction: Both
  - Indicators: Bollinger Bands, Heikin Ashi, EMA
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
