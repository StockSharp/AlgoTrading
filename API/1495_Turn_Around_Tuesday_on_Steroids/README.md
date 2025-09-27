# Turn Around Tuesday on Steroids Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

A seasonal long strategy that buys after two consecutive down days at the start of the week and exits on a breakout above the previous high. An optional moving average filter confirms trend direction.

## Details

- **Entry Criteria**: first or second day of the week with two-day decline
- **Long/Short**: Long
- **Exit Criteria**: close above prior high
- **Stops**: None
- **Default Values**:
  - `StartingDay` = Sunday
  - `MaPeriod` = 200
- **Filters**:
  - Category: Seasonality
  - Direction: Long
  - Indicators: SMA
  - Stops: No
  - Complexity: Beginner
  - Timeframe: Daily
  - Seasonality: Yes
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
