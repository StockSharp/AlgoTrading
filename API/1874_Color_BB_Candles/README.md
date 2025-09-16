# Color BB Candles Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses Bollinger Bands to classify candles into bullish, bearish, or neutral zones. It opens a long position when price closes above the upper band, opens a short position when price closes below the lower band, and exits any position when price returns between the bands.

## Details

- **Entry Criteria**:
  - **Long**: Close price crosses above the upper band from outside.
  - **Short**: Close price crosses below the lower band from outside.
- **Exit Criteria**: Price returns between the upper and lower bands.
- **Indicators**: Bollinger Bands.
- **Default Values**:
  - `BollingerPeriod` = 100
  - `BollingerDeviation` = 1.0
  - `CandleType` = 4-hour timeframe
- **Direction**: Long and short.
- **Stops**: None.
- **Complexity**: Medium.
- **Timeframe**: Medium-term.

