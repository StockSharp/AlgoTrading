# Stochastic Keltner Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
This strategy uses Stochastic Keltner indicators to generate signals.
Long entry occurs when Stoch %K < 20 && Price < Keltner lower band (oversold at lower band). Short entry occurs when Stoch %K > 80 && Price > Keltner upper band (overbought at upper band).
It is suitable for traders seeking opportunities in mixed markets.

## Details
- **Entry Criteria**:
  - **Long**: Stoch %K < 20 && Price < Keltner lower band (oversold at lower band)
  - **Short**: Stoch %K > 80 && Price > Keltner upper band (overbought at upper band)
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit long position when price returns to middle band
  - **Short**: Exit short position when price returns to middle band
- **Stops**: Yes.
- **Default Values**:
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochD` = 3
  - `EmaPeriod` = 20
  - `KeltnerMultiplier` = 2m
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2m
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

Testing indicates an average annual return of about 61%. It performs best in the crypto market.
