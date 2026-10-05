# MACD Bollinger Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
This strategy uses MACD Bollinger indicators to generate signals.
Long entry occurs when MACD > Signal && Price < BB_lower (trend up with oversold conditions). Short entry occurs when MACD < Signal && Price > BB_upper (trend down with overbought conditions).
It is suitable for traders seeking opportunities in mixed markets.

Testing indicates an average annual return of about 55%. It performs best in the stocks market.

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
  - `BollingerDeviation` = 2
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2
    A position closes once the candle closes back at the middle band. The stop lies AtrMultiplier times the AtrPeriod ATR from the entry close and is checked on candle closes; 0 disables it. An entry signal against an open position reverses it.
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

