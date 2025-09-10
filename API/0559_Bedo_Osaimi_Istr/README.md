# Bedo Osaimi Istr Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

A simple trend-following strategy that compares moving averages of close and open prices. A long position is opened when the moving average of close crosses above the moving average of open. The position is reversed when the opposite cross occurs.

## Details

- **Entry Criteria**:
  - Close MA crosses above open MA.
- **Long/Short**: Both directions.
- **Exit Criteria**:
  - Close MA crosses below open MA (for long exit or short entry).
- **Stops**: None.
- **Default Values**:
  - `MaLength` = 20
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: SMA on close and open
  - Stops: No
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
