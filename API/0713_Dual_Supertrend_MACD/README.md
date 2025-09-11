# Dual Supertrend MACD
[Русский](README_ru.md) | [中文](README_cn.md)

Dual Supertrend MACD combines two Supertrend indicators with a MACD filter.
A long position is opened when price trades above both Supertrend lines and the MACD histogram is positive.
Short positions appear when price is below both lines and the histogram is negative.
Positions are closed once any Supertrend flips direction or the MACD histogram crosses zero.

## Details
- **Data**: Price candles.
- **Entry Criteria**:
  - Long: `Close > Supertrend1 && Close > Supertrend2 && MACD Histogram > 0`
  - Short: `Close < Supertrend1 && Close < Supertrend2 && MACD Histogram < 0`
- **Exit Criteria**:
  - Long: `Close < Supertrend1 || Close < Supertrend2 || MACD Histogram < 0`
  - Short: `Close > Supertrend1 || Close > Supertrend2 || MACD Histogram > 0`
- **Stops**: None by default.
- **Default Values**:
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `OscillatorMaType` = Exponential
  - `SignalMaType` = Exponential
  - `AtrPeriod1` = 10
  - `Factor1` = 3.0
  - `AtrPeriod2` = 20
  - `Factor2` = 5.0
  - `TradeDirection` = "Both"
- **Filters**:
  - Category: Trend following
  - Direction: Configurable
  - Indicators: Supertrend, MACD
  - Complexity: Intermediate
  - Risk level: Medium
