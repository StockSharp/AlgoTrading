# Color Schaff RSI Trend Cycle Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Trend-following system based on the Color Schaff RSI Trend Cycle (STC) oscillator. The strategy reacts to color transitions of the STC indicator to enter and exit both long and short positions.

## Details

- **Entry Criteria**:
  - **Long**: Indicator color two bars ago > 5 and last bar < 6.
  - **Short**: Indicator color two bars ago < 2 and last bar > 1.
- **Long/Short**: Both directions.
- **Exit Criteria**:
  - Long positions close when indicator color two bars ago < 2.
  - Short positions close when indicator color two bars ago > 5.
- **Indicators**: Color Schaff RSI Trend Cycle.
- **Default Values**:
  - `Fast RSI` = 23
  - `Slow RSI` = 50
  - `Cycle` = 10
  - `High Level` = 60
  - `Low Level` = -60
- **Timeframe**: 4-hour candles by default.
- **Stops**: None.
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Single
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
