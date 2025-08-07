# Heikin Ashi V2 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This second version of the Heikin Ashi system adds an EMA filter. Trades occur only when the direction of the Heikin Ashi candle agrees with the trend defined by the EMA. The filter helps avoid counter-trend signals that the pure HA approach might generate.

## Details

- **Entry Criteria**:
  - **Long**: `HA_Close > HA_Open` and `Close > EMA`
  - **Short**: `HA_Close < HA_Open` and `Close < EMA`
- **Long/Short**: Both sides
- **Exit Criteria**:
  - Opposite signal
- **Stops**: None
- **Default Values**:
  - `EmaLength` = 20
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Heikin Ashi, EMA
  - Stops: No
  - Complexity: Low
  - Timeframe: Short-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
