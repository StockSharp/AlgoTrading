# BTC Future Gamma-Weighted Momentum Model
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy computes a gamma-weighted average price (GWAP) to capture momentum in BTC futures. Long trades are opened when price stays above the GWAP and the last three closes rise consecutively. Short positions are taken when price is below the GWAP and the last three closes fall consecutively.

## Details

- **Entry Criteria**:
  - **Long**: Close above GWAP and last three closes rising.
  - **Short**: Close below GWAP and last three closes falling.
- **Long/Short**: Both sides.
- **Exit Criteria**: Opposite signal.
- **Stops**: None.
- **Default Values**:
  - `Length` = 60
  - `GammaFactor` = 0.75
- **Filters**:
  - Category: Momentum
  - Direction: Both
  - Indicators: GWAP
  - Stops: None
  - Complexity: Low
  - Timeframe: 1m
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
