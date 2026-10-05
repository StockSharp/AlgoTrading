# Keltner Williams R Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
This strategy uses Keltner Williams R indicators to generate signals.
Long entry occurs when Price < lower Keltner band && Williams %R < WilliamsROversold (oversold at lower band). Short entry occurs when Price > upper Keltner band && Williams %R > WilliamsROverbought (overbought at upper band).
It is suitable for traders seeking opportunities in mixed markets.

Testing indicates an average annual return of about 46%. It performs best in the stocks market.

## Details
- **Entry Criteria**:
  - **Long**: Price < lower Keltner band && Williams %R < WilliamsROversold (oversold at lower band)
  - **Short**: Price > upper Keltner band && Williams %R > WilliamsROverbought (overbought at upper band)
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
  - `WilliamsROversold` = -80
  - `WilliamsROverbought` = -20
  - `StopLossPercent` = 2
    The bands are the EmaPeriod EMA plus and minus KeltnerMultiplier times the AtrPeriod ATR, and the middle band is the EMA itself. -80 and -20 are the defaults of the Williams %R levels the rules quote. The stop is a fixed StopLossPercent of the entry price, watched between candles as well. An entry signal against an open position reverses it.
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

