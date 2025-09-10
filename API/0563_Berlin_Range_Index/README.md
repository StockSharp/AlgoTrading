# Berlin Range Index Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Berlin Range Index strategy filters the standard Choppiness Index with an ATR based factor to highlight trending and ranging phases. When the filtered index falls below a minimum threshold the strategy opens a position in the direction of the current candle. Positions are closed when the index indicates a ranging or weakening trend.

## Details

- **Entry Criteria**:
  - Filtered range index below `ChopMin` and candle direction defines long or short.
- **Exit Criteria**:
  - Range index above `ChopMax` or weakening trend.
- **Stops**: None.
- **Default Values**:
  - `Length` = 9
  - `ChopMax` = 40
  - `ChopMin` = 10
  - `AtrLength` = 14
  - `LowLookback` = 14
  - `UseNormalized` = true
  - `StdDevLength` = 14
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Choppiness Index, ATR, Standard Deviation
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
