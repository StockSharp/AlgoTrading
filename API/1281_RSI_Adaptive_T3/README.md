# RSI Adaptive T3
[Русский](README_ru.md) | [中文](README_cn.md)

Trend-following strategy based on an RSI-adaptive Tillson T3 moving average. It goes long when the T3 crosses above its two-bar lag and exits on the opposite cross.

Backtests on daily charts show steady performance in trending markets.

## Details

- **Entry Criteria**: T3 crossing above its 2-bar lag.
- **Long/Short**: Long only.
- **Exit Criteria**: Opposite cross.
- **Stops**: No.
- **Default Values**:
  - `RsiLength` = 14
  - `MinT3Length` = 5
  - `MaxT3Length` = 50
  - `VolumeFactor` = 0.7
  - `CandleType` = TimeSpan.FromDays(1)
- **Filters**:
  - Category: Trend Following
  - Direction: Long
  - Indicators: RSI, T3
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Daily
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
