# Donchian HL Width Cycle Information
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that trades based on Donchian channel width and cycle changes.

The strategy monitors the relation of candle extremes to the Donchian channel. After a down cycle, hitting the upper band opens a long position. After an up cycle, touching the lower band opens a short position.

## Details

- **Entry Criteria**: Cycle trend change on Donchian channel.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite cycle signal.
- **Stops**: No.
- **Default Values**:
  - `Length` = 28
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Donchian
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
