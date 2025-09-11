# Trend Following MM3 High Low
[Русский](README_ru.md) | [中文](README_cn.md)

Uses a 3-period simple moving average of highs and lows. A long trade opens when price closes above the SMA of highs and exits when price falls below the SMA of lows.

## Details

- **Entry Criteria**: Close > SMA(high).
- **Long/Short**: Long only.
- **Exit Criteria**: Close < SMA(low).
- **Stops**: No.
- **Default Values**:
  - `Length` = 3
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Trend
  - Direction: Long
  - Indicators: SMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (1m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
