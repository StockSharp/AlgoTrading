# Bollinger Bands SMA 20-2 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses Bollinger Bands built from a 20-period simple moving average with a 2 standard deviation multiplier. It goes long when price crosses above the lower band and goes short when price crosses below the upper band. Positions reverse on opposite signals without explicit stop-losses.

## Details

- **Entry Criteria**:
  - **Long**: `Close` crosses above lower band.
  - **Short**: `Close` crosses below upper band.
- **Long/Short**: Both directions.
- **Exit Criteria**:
  - Opposite signal.
- **Stops**: None.
- **Default Values**:
  - `Bollinger Length` = 20
  - `Bollinger Multiplier` = 2
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: Single
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
