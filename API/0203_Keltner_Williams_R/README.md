# Keltner Williams R Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
This strategy uses Keltner Williams R indicators to generate signals.
Long entry occurs when Price < lower Keltner band && Williams %R < -80 (oversold at lower band). Short entry occurs when Price > upper Keltner band && Williams %R > -20 (overbought at upper band).
It is suitable for traders seeking opportunities in mixed markets.

## Details
- **Entry Criteria**:
  - **Long**: Price < lower Keltner band && Williams %R < -80 (oversold at lower band)
  - **Short**: Price > upper Keltner band && Williams %R > -20 (overbought at upper band)
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit long position when price returns to middle band
  - **Short**: Exit short position when price returns to middle band
- **Stops**: Yes.
- **Default Values**:
  - `EmaPeriod` = 20
  - `KeltnerMultiplier` = 2m
  - `AtrPeriod` = 14
  - `WilliamsRPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Mixed
  - Direction: Both
  - Indicators: Keltner Williams R
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium

Testing indicates an average annual return of about 46%. It performs best in the stocks market.
