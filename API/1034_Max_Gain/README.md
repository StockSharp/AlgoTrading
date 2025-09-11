# Max Gain
[Русский](README_ru.md) | [中文](README_cn.md)

Max Gain compares the percentage distance from the lowest low to the current high and from the highest high to the current low over a lookback period. It goes long when potential gain exceeds the adjusted loss, otherwise it goes short.

## Details
- **Data**: Price candles.
- **Entry Criteria**:
  - **Long**: Max gain > adjusted max loss.
  - **Short**: Adjusted max loss > max gain.
- **Exit Criteria**: Reverse signal.
- **Stops**: None.
- **Default Values**:
  - `PeriodLength` = 30
- **Filters**:
  - Category: Momentum
  - Direction: Long & Short
  - Indicators: Highest, Lowest
  - Complexity: Low
  - Risk level: Medium
