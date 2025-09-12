# VoVix DEVMA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy analyzes volatility behaviour using Deviation Moving Averages (DEVMA) built on the standard deviation of ATR. It trades transitions between contraction and expansion regimes and uses ATR-based exits.

## Details

- **Entry Criteria**:
  - **Long**: Fast DEVMA crosses above Slow DEVMA.
  - **Short**: Fast DEVMA crosses below Slow DEVMA.
- **Long/Short**: Both.
- **Exit Criteria**:
  - ATR stop-loss and take-profit.
- **Stops**: Yes, ATR multiples.
- **Default Values**:
  - `DeviationLookback` = 59
  - `FastLength` = 20
  - `SlowLength` = 60
  - `ATR SL Mult` = 2
  - `ATR TP Mult` = 3
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Multiple
  - Stops: Yes
  - Complexity: Complex
  - Timeframe: Medium-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
