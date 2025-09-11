# Dskyz (DAFE) GENESIS Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Simplified version of the Dskyz (DAFE) GENESIS strategy. The system trades when short-term momentum aligns with a trend filter and RSI.

## Details

- **Entry Criteria**:
  - **Long**: `SMA(9) > SMA(30)` and `RSI > 55` and `EMA(8) > EMA(21)`.
  - **Short**: `SMA(9) < SMA(30)` and `RSI < 45` and `EMA(8) < EMA(21)`.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: `EMA(8) < EMA(21)`.
  - **Short**: `EMA(8) > EMA(21)`.
- **Stops**: None.
- **Default Values**:
  - `RSI Length` = 9.
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: RSI, EMA, SMA
  - Stops: No
  - Complexity: Simple
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
