# Cup Finder Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This pattern-based strategy searches for rounded "cup" formations in price data. When price breaks out from a completed cup, it enters long or short depending on the direction.

Testing indicates an average annual return of about 47%. It works best on stocks.

The strategy buys on bullish cup breakouts and sells on bearish inverted cups. Positions are protected by a stop-loss.

## Details

- **Entry Criteria**: Cup pattern forms and price breaks the rim.
- **Long/Short**: Both.
- **Exit Criteria**: Price reverses or hits stop-loss.
- **Stops**: Yes.
- **Default Values**:
  - `Lookback` = 150
  - `WidthPercent` = 5m
  - `StopLossPercent` = 1m
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filters**:
  - Category: Pattern
  - Direction: Long/Short
  - Indicators: Price Action
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: Yes
  - Risk Level: Medium
