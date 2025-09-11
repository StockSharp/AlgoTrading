# S4 IBS Mean Rev 3candleExit Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy buys when the previous candle's Internal Bar Strength (IBS) is below a threshold, expecting mean reversion. It exits when price closes above the entry or after three candles if the trade remains losing.

## Details

- **Entry Criteria**: previous IBS <= threshold
- **Long/Short**: Long only
- **Exit Criteria**: close above entry price or after 3 candles if still below entry; force exit after end time
- **Stops**: No
- **Default Values**:
  - `IbsThreshold` = 0.25
  - `StartTime` = 2024-01-01 05:00:00 UTC
  - `EndTime` = 2024-12-31 00:00:00 UTC
- **Filters**:
  - Category: Mean Reversion
  - Direction: Long
  - Indicators: None
  - Stops: No
  - Complexity: Basic
  - Timeframe: Daily
  - Seasonality: Yes
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
