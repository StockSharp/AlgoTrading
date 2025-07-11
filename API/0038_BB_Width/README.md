# Bollinger Band Width Breakout

Bollinger Band Width measures the spread between the upper and lower bands. Expanding width suggests volatility and possible trend formation. This strategy trades breakouts when the width is increasing.

Price position relative to the middle band sets direction. A widening channel with price above the mid-band triggers longs, while a widening channel below it triggers shorts.

Exits occur when the band width contracts or a volatility stop is reached.

## Rules

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
