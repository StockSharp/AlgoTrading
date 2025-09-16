# RSI Bollinger Bands
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy combining the Relative Strength Index (RSI) with Bollinger Bands. A long position opens when RSI is below the oversold threshold and the close price is under the lower Bollinger band. A short position opens when RSI is above the overbought threshold and the close price is above the upper Bollinger band. Positions reverse on opposite signals.

## Details

- **Entry Criteria**: RSI below `RsiOversold` and close price below lower band for buy; RSI above `RsiOverbought` and close price above upper band for sell.
- **Long/Short**: Both directions.
- **Exit Criteria**: Reverse signal.
- **Stops**: None.
- **Default Values**:
  - `CandleType` = TimeSpan.FromMinutes(15)
  - `RsiPeriod` = 20
  - `BollingerPeriod` = 20
  - `BollingerWidth` = 2
  - `RsiOversold` = 30
  - `RsiOverbought` = 70
- **Filters**:
  - Category: Oscillator
  - Direction: Both
  - Indicators: RSI, Bollinger Bands
  - Stops: No
  - Complexity: Basic
  - Timeframe: 15 minutes
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
