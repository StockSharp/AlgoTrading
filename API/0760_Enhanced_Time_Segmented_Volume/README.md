# Enhanced Time Segmented Volume
[Русский](README_ru.md) | [中文](README_cn.md)

Enhanced Time Segmented Volume monitors volume‑weighted price changes. When the TSV is above its moving average and positive, the strategy buys. When the TSV is below the average and negative, it sells short.

## Details

- **Entry Criteria**: TSV relative to its moving average.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or stop.
- **Stops**: Yes.
- **Default Values**:
  - `TsvLength` = 13
  - `MaLength` = 7
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Momentum
  - Direction: Both
  - Indicators: Volume, SMA
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
