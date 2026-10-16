# Price and Volume Breakout Buy Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

The strategy enters when price and volume simultaneously break above their respective lookback highs while price stays above the trend SMA. Short trades trigger when price drops below the lookback low under the same volume condition and SMA filter. Positions close after five consecutive closes on the opposite side of the SMA.

## Details
- **Entry Criteria**:
  - **Long**: Close > previous highest high && Volume > previous highest volume && Close > SMA
  - **Short**: Close < previous lowest low && Volume > previous highest volume && Close < SMA
- **Long/Short**: Configurable
- **Exit Criteria**:
  - **Trend**: Five closes beyond SMA
- **Stops**: No
- **Default Values**:
  - `PriceBreakoutPeriod` = 60
  - `VolumeBreakoutPeriod` = 60
  - `TrendlineLength` = 200
  - `OrderDirection` = "Long"
  - `CandleType` = TimeSpan.FromDays(1)
- **Filters**:
  - Category: Breakout
  - Direction: Configurable
  - Indicators: Highest, SMA, Volume
  - Stops: No
  - Complexity: Basic
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium

The price and volume extrema use only preceding completed candles (the signal candle is excluded). `OrderDirection` accepts `Long`, `Short`, or `Both`; the default is `Long`. A close on the SMA, or back on the position's side of it, resets the five-close exit count.
