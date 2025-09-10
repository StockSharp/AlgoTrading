# Buy On 5 Day Low
[Русский](README_ru.md) | [中文](README_cn.md)

The **Buy On 5 Day Low** strategy goes long when the close drops below the previous 5-day low. It exits when the close rises above the prior bar's high. Trading is limited to a configurable time window.

## Details
- **Entry Criteria**: Close falls below the previous lowest low over the last N candles.
- **Long/Short**: Long only.
- **Exit Criteria**: Close exceeds the previous high.
- **Stops**: No.
- **Default Values**:
  - `LowestPeriod = 5`
  - `CandleType = TimeSpan.FromDays(1).TimeFrame()`
  - `StartTime = new DateTimeOffset(2014, 1, 1, 0, 0, 0, TimeSpan.Zero)`
  - `EndTime = new DateTimeOffset(2099, 1, 1, 0, 0, 0, TimeSpan.Zero)`
- **Filters**:
  - Category: Mean reversion
  - Direction: Long
  - Indicators: Lowest, High
  - Stops: No
  - Complexity: Basic
  - Timeframe: Daily
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
