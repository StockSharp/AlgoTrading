# SPY TLT Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy buys the main security when the TLT price crosses above its SMA and exits when TLT closes below the SMA. Trading is allowed only within the specified time window.

## Details

- **Entry Criteria**:
  - **Long**: TLT closes above its SMA within the time window.
- **Long/Short**: Long only.
- **Exit Criteria**:
  - TLT closes below its SMA.
- **Stops**: None.
- **Default Values**:
  - `Start Time` = 2014-01-01
  - `End Time` = 2099-01-01
  - `TLT Symbol` = TLT
  - `SMA Length` = 20
- **Filters**:
  - Category: Trend following
  - Direction: Long
  - Indicators: SMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
