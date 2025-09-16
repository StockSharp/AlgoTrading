# Price Channel Stop
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy based on the Price Channel Stop indicator.

The indicator calculates the highest high and lowest low over the given period to form a Donchian channel. Stop levels are built inside the channel using the `Risk` factor. When the price closes above the upper stop the trend switches to bullish; closing below the lower stop switches the trend to bearish. The strategy opens positions on these reversals and optionally closes opposite positions.

## Details

- **Entry Criteria**: Price crosses stop levels.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal.
- **Stops**: No.
- **Default Values**:
  - `ChannelPeriod` = 5
  - `Risk` = 0.10
  - `CandleType` = TimeSpan.FromHours(1)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Donchian Channel
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (1h)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

