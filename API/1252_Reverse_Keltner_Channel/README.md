# Reverse Keltner Channel Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that enters when price re-enters the Keltner channel from outside and aims for the opposite band, with optional ADX filter.

The strategy goes long when price crosses the lower Keltner band from below and closes at the upper band or at a stop placed at half of the channel width. Short trades are symmetrical. An ADX filter can restrict trades to weak or strong trend regimes.

## Details

- **Entry Criteria**: Price crosses outer Keltner band into channel, optional ADX filter.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite band or stop.
- **Stops**: Yes.
- **Default Values**:
  - `EmaPeriod` = 20
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 2m
  - `StopLossFactor` = 0.5m
  - `AdxLength` = 14
  - `AdxThreshold` = 25m
  - `UseAdxFilter` = true
  - `WeakTrendOnly` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Reversal
  - Direction: Both
  - Indicators: Keltner, ADX
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
