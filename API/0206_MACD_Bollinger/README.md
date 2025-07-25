# MACD Bollinger Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
This strategy uses MACD Bollinger indicators to generate signals.
Long entry occurs when MACD > Signal && Price < BB_lower (trend up with oversold conditions). Short entry occurs when MACD < Signal && Price > BB_upper (trend down with overbought conditions).
It is suitable for traders seeking opportunities in mixed markets.

## Details
- **Entry Criteria**:
  - **Long**: MACD > Signal && Price < BB_lower (trend up with oversold conditions)
  - **Short**: MACD < Signal && Price > BB_upper (trend down with overbought conditions)
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit long position when price returns to middle band
  - **Short**: Exit short position when price returns to middle band
- **Stops**: Yes.
- **Default Values**:
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Mixed
  - Direction: Both
  - Indicators: MACD Bollinger
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium

Testing indicates an average annual return of about 55%. It performs best in the stocks market.
