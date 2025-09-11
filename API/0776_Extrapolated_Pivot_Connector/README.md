# Extrapolated Pivot Connector Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Connects recent pivot highs and lows to build support and resistance lines. A long signal occurs when price closes above the resistance line, while a short signal fires when price drops below the support line.

## Details

- **Entry Criteria**:
  - **Long**: Close crosses above resistance line.
  - **Short**: Close crosses below support line.
- **Exit Criteria**:
  - Opposite signal or position reversal.
- **Indicators**:
  - Pivot-based support/resistance lines.
- **Stops**: None.
- **Default Values**:
  - `PivotLength` = 100
  - `HighStart` = 1
  - `HighEnd` = 0
  - `LowStart` = 1
  - `LowEnd` = 0
- **Filters**:
  - Trend-following
  - Single timeframe
  - Indicators: pivot lines
  - Stops: none
  - Complexity: Low
