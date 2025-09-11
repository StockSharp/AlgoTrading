# Connors VIX Reversal III
[Русский](README_ru.md) | [中文](README_cn.md)

Contrarian strategy using VIX spikes relative to its moving average. It buys when VIX jumps above the average by a set percentage and shorts when VIX drops below it.

Positions close when VIX crosses the previous day's moving average.

## Details

- **Entry Criteria**: VIX low above MA and close above MA by threshold for buys; VIX high below MA and close below threshold for sells.
- **Long/Short**: Both.
- **Exit Criteria**: VIX crossing yesterday's MA.
- **Stops**: No.
- **Default Values**:
  - `LengthMA` = 10
  - `PercentThreshold` = 10m
  - `CandleType` = TimeSpan.FromDays(1)
- **Filters**:
  - Category: Contrarian
  - Direction: Both
  - Indicators: VIX, SMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Daily
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
