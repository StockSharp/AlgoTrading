# Stochastic RSI OHLC Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy builds OHLC bars from the Stochastic RSI indicator and trades on momentum shifts. It calculates RSI for high, low and close prices and applies a stochastic oscillator to each series. A long position opens when Stochastic RSI rises from a pivot and crosses above the long entry level. A short position opens when it falls from a pivot and crosses below the short entry level.

## Details

- **Entry Criteria**:
  - **Long**: Stochastic RSI turns up and any of the last three values exceed `LongEntry` after a low pivot.
  - **Short**: Stochastic RSI turns down and any of the last three values fall below `ShortEntry` after a high pivot.
- **Long/Short**: Both sides.
- **Exit Criteria**: Opposite signal.
- **Stops**: None.
- **Default Values**:
  - `RSI Length` = 14
  - `K Length` = 14
  - `D Length` = 3
  - `LongEntry` = 30
  - `ShortEntry` = 60
  - `LongPivot` = 2
  - `ShortPivot` = 98
- **Filters**:
  - Category: Momentum
  - Direction: Both
  - Indicators: RSI, Stochastic
  - Stops: No
  - Complexity: Medium
  - Timeframe: Short-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
