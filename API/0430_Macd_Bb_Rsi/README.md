# MACD + Bollinger Bands + RSI Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This composite setup looks for pullbacks against the prevailing MACD momentum that stretch beyond the Bollinger Bands. When MACD is positive yet price closes below the lower band with an oversold RSI, the strategy buys in anticipation of a trend continuation. The opposite applies for shorts.

## Details

- **Entry Criteria**:
  - **Long**: `MACD > 0` and `Close < LowerBand` and `RSI < 30`
  - **Short**: `MACD < 0` and `Close > UpperBand` and `RSI > 70`
- **Long/Short**: Both sides
- **Exit Criteria**: Opposite signal
- **Stops**: None
- **Default Values**:
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `BBLength` = 20
  - `BBMultiplier` = 2.0
  - `RSILength` = 14
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: MACD, Bollinger Bands, RSI
  - Stops: No
  - Complexity: Medium
  - Timeframe: Short-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: Yes
  - Risk level: Medium
