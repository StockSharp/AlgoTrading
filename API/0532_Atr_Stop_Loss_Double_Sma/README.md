# ATR Stop Loss Double SMA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy enters long when a fast Simple Moving Average (SMA) crosses above a slow SMA and enters short on the opposite cross.
An optional stop-loss uses the Average True Range (ATR) multiplied by a user-defined factor to determine exit levels.

## Details

- **Entry Criteria**:
  - **Long**: Fast SMA crosses above slow SMA.
  - **Short**: Fast SMA crosses below slow SMA.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - ATR-based stop-loss if enabled.
- **Stops**: ATR multiple from entry price.
- **Default Values**:
  - `FastLength` = 15
  - `SlowLength` = 45
  - `AtrLength` = 14
  - `AtrMultiplier` = 2
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: SMA, ATR
  - Stops: Yes
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
