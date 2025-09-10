# 200 SMA Buffer Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The 200 SMA Buffer strategy trades based on the price's distance from a long-term Simple Moving Average. It buys when the close rises a certain percentage above the SMA and exits when the price falls a defined percentage below it. The approach aims to capture long-term momentum while allowing a buffer around the moving average.

## Details

- **Entry Criteria**:
  - Close price > SMA * (1 + Entry %).
- **Long/Short**: Long only.
- **Exit Criteria**:
  - Close price < SMA * (1 - Exit %).
- **Stops**: None.
- **Default Values**:
  - `SmaLength` = 200
  - `EntryPercent` = 5
  - `ExitPercent` = 3
- **Filters**:
  - Category: Trend following
  - Direction: Long
  - Indicators: SMA
  - Stops: No
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
