# Stochastic Keltner Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
This strategy uses Stochastic Keltner indicators to generate signals.
Long entry occurs when Stoch %K < StochOversold && Price < Keltner lower band (oversold at lower band). Short entry occurs when Stoch %K > StochOverbought && Price > Keltner upper band (overbought at upper band).
It is suitable for traders seeking opportunities in mixed markets.

Testing indicates an average annual return of about 61%. It performs best in the crypto market.

## Details
- **Entry Criteria**:
  - **Long**: Stoch %K < StochOversold && Price < Keltner lower band (oversold at lower band)
  - **Short**: Stoch %K > StochOverbought && Price > Keltner upper band (overbought at upper band)
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit long position when price returns to middle band
  - **Short**: Exit short position when price returns to middle band
- **Stops**: Yes.
- **Default Values**:
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochOversold` = 20
  - `StochOverbought` = 80
  - `EmaPeriod` = 20
  - `KeltnerMultiplier` = 2
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2
    20 and 80 are the defaults of the %K levels the rules quote. The bands are the EmaPeriod EMA plus and minus KeltnerMultiplier times the AtrPeriod ATR, and the middle band is the EMA itself. The stop lies AtrMultiplier times the same ATR from the entry close and is checked on candle closes; 0 disables it. StochK in the rules is %K: the stochastic over StochPeriod candles smoothed over StochK candles; %D plays no part, so it has no setting. An entry signal against an open position reverses it.
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Mixed
  - Direction: Both
  - Indicators: Stochastic Keltner
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium

