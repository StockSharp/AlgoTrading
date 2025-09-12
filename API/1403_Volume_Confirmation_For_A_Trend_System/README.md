# Volume Confirmation For A Trend System Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses the Trend Thrust Indicator (TTI), Volume Price Confirmation Indicator (VPCI) and ADX to confirm long trends.

## Details
- **Entry Criteria**:
  - **Long**: ADX > 30, TTI > signal, VPCI > 0.
- **Long/Short**: Long only.
- **Exit Criteria**:
  - VPCI < 0.
- **Stops**: No.
- **Default Values**:
  - `ADX Length` = 14
  - `ADX Smoothing` = 14
  - `TTI Fast Average` = 13
  - `TTI Slow Average` = 26
  - `TTI Signal Length` = 9
  - `VPCI Short Avg` = 5
  - `VPCI Long Avg` = 25
- **Filters**:
  - Category: Trend
  - Direction: Long
  - Indicators: ADX, TTI, VPCI
  - Stops: No
  - Complexity: Medium
  - Timeframe: Medium-term
