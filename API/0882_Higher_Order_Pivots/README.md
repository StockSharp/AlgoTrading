# Higher Order Pivots Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Detects first, second and third order pivot highs and lows using either 3-bar or 5-bar pivot definitions. The strategy is analytical and does not place orders.

## Details

- **Entry Criteria**:
  - None (analysis only).
- **Exit Criteria**:
  - None.
- **Indicators**:
  - 3-bar or 5-bar pivot detector.
- **Stops**: None.
- **Default Values**:
  - `CandleType` = 5m
  - `UseThreeBar` = true
  - `DisplayFirstOrder` = true
  - `DisplaySecondOrder` = true
  - `DisplayThirdOrder` = true
- **Filters**:
  - Single timeframe
  - Indicators: pivot detector
  - Stops: none
  - Complexity: Low
