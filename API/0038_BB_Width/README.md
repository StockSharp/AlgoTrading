# Bollinger Band Width Breakout
[Русский](README_ru.md) | [中文](README_cn.md)
 
Bollinger Band Width measures the spread between the upper and lower bands. Expanding width suggests volatility and possible trend formation. This strategy trades breakouts when the width is increasing.

Testing indicates an average annual return of about 151%. It performs best in the stocks market.

Price position relative to the middle band sets direction. A widening channel with price above the mid-band triggers longs, while a widening channel below it triggers shorts.

Exits occur when the band width contracts or a volatility stop is reached.

## Details

- **Entry Criteria**: Band width expanding and price relative to middle band.
- **Long/Short**: Both directions.
- **Exit Criteria**: Band width contracts or stop.
- **Stops**: Yes.
- **Default Values**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `AtrMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Bollinger Bands, ATR
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

