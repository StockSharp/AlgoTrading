# Multi-Band Comparison
[Русский](README_ru.md) | [中文](README_cn.md)

Multi-Band Comparison uses SMA, standard deviation and price quantile bands. The strategy goes long when price closes above the upper quantile minus standard deviation for a defined number of bars and exits when price falls below that level for a set number of bars.

## Details
- **Data**: Price candles.
- **Entry Criteria**:
  - **Long**: Close above (upper quantile - std dev) for `EntryConfirmBars` bars.
- **Exit Criteria**: Close below that line for `ExitConfirmBars` bars.
- **Stops**: None.
- **Default Values**:
  - `Length` = 20
  - `BollingerMultiplier` = 1
  - `UpperQuantile` = 0.95
  - `EntryConfirmBars` = 1
  - `ExitConfirmBars` = 1
- **Filters**:
  - Category: Statistical
  - Direction: Long
  - Indicators: SMA, Standard Deviation
  - Complexity: Medium
  - Risk level: Medium
