# Bar Counter Trend Reversal Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Looks for several consecutive rising or falling bars and takes counter-trend trades when price reaches channel extremes.

## Details

- **Entry Criteria**: series of rises or falls with optional volume and channel confirmation
- **Long/Short**: Both
- **Exit Criteria**: opposite signal
- **Stops**: No
- **Default Values**:
  - `NoOfRises` = 3
  - `NoOfFalls` = 3
  - `ChannelLength` = 20
  - `ChannelMultiplier` = 2
- **Filters**:
  - Category: Reversal
  - Direction: Both
  - Indicators: Keltner Channel or Bollinger Bands
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
