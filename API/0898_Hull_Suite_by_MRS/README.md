# Hull Suite by MRS Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

A trend-following strategy that compares the selected Hull-type moving average with its value two bars ago. Long positions open when the average rises above its two-bar-old value, and short positions when it falls below.

## Details

- **Entry Criteria**:
  - **Long**: `MA > MA[2]`.
  - **Short**: `MA < MA[2]`.
- **Long/Short**: Both directions.
- **Exit Criteria**: Reverse on opposite signal.
- **Stops**: None.
- **Default Values**:
  - `Length` = 55
  - `Mode` = Hma
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Hull MA
  - Stops: None
  - Complexity: Simple
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
