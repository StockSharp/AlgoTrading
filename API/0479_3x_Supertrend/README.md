# 3x Supertrend
[Русский](README_ru.md) | [中文](README_cn.md)

The **3x Supertrend** strategy uses three ATR-based bands with different periods and multipliers.
A long position is opened when price rises above all three bands and the fast band switches to
uptrend. The trade is closed when price falls below every band, signaling loss of bullish momentum.

## Details
- **Data**: Price candles.
- **Entry Criteria**: Price above all bands and fast band turning up.
- **Exit Criteria**: Price below all bands.
- **Stops**: None.
- **Default Values**:
  - `AtrPeriod1` = 11
  - `Factor1` = 1
  - `AtrPeriod2` = 12
  - `Factor2` = 2
  - `AtrPeriod3` = 13
  - `Factor3` = 3
- **Filters**:
  - Category: Trend following
  - Direction: Long only
  - Indicators: ATR-based Supertrend
  - Complexity: Medium
  - Risk level: Medium
