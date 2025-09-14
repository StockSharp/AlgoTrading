# Exponential MA Rounding Channel Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy rounds a moving average to a fixed tick step and builds an ATR-based channel around it. When the previous candle closes above the upper band, the strategy opens a long position. When the previous candle closes below the lower band, it opens a short position. Opposite signals close existing positions. Stop loss and take profit are defined in ticks and managed automatically.

## Details

- **Entry Criteria**:
  - **Long**: Previous close is above the upper rounded band.
  - **Short**: Previous close is below the lower rounded band.
- **Exit Criteria**:
  - **Long**: Previous close is below the lower band.
  - **Short**: Previous close is above the upper band.
- **Indicators**:
  - Exponential Moving Average.
  - Average True Range for channel width.
- **Stops**: Yes, fixed stop loss and take profit in ticks.
- **Default Values**:
  - `MA period` = 12.
  - `ATR period` = 12.
  - `ATR factor` = 1.
  - `MA round` = 500 ticks.
  - `Stop loss` = 1000 ticks.
  - `Take profit` = 2000 ticks.
  - `Timeframe` = 4 hours.

## Filters

- Category: Trend following
- Direction: Both
- Indicators: Multiple
- Stops: Yes
- Complexity: Medium
- Timeframe: Medium-term
- Seasonality: No
- Neural networks: No
- Divergence: No
- Risk level: Moderate
