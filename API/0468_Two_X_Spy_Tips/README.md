# Two X SPY TIPS
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy allocates capital into the traded asset when both S&P 500 and TIPS prices are above their 200-period moving averages at the turn of a new month.

## Details

- **Entry Criteria**: S&P 500 and TIPS above their SMA at a new month.
- **Long/Short**: Long only.
- **Exit Criteria**: No exits.
- **Stops**: No.
- **Default Values**:
  - `SmaLength` = 200
  - `Leverage` = 2
  - `CandleType` = TimeSpan.FromDays(1)
- **Filters**:
  - Category: Trend
  - Direction: Long only
  - Indicators: SMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Daily
  - Seasonality: Yes
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
