# HMA 200 + EMA 20 Crossover Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy enters long when price is above the 200-period Hull Moving Average
and crosses above the 20-period Exponential Moving Average. Short positions are
opened when price is below the HMA and crosses below the EMA. Positions reverse
on opposite signals.

## Details

- **Entry Criteria**:
  - **Long**: `Close > HMA` and `Close` crosses above `EMA`.
  - **Short**: `Close < HMA` and `Close` crosses below `EMA`.
- **Long/Short**: Both directions.
- **Exit Criteria**: Reverse on opposite crossover signal.
- **Stops**: None.
- **Default Values**:
  - `HMA Length` = 200
  - `EMA Length` = 20
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: HMA, EMA
  - Stops: None
  - Complexity: Simple
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
