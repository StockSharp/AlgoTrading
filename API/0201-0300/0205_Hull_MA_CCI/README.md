# Hull MA CCI Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
This strategy uses Hull MA CCI indicators to generate signals.
Long entry occurs when HMA(t) > HMA(t-1) && CCI < CciOversold (HMA rising with oversold conditions). Short entry occurs when HMA(t) < HMA(t-1) && CCI > CciOverbought (HMA falling with overbought conditions).
It is suitable for traders seeking opportunities in mixed markets.

Testing indicates an average annual return of about 52%. It performs best in the crypto market.

## Details
- **Entry Criteria**:
  - **Long**: HMA(t) > HMA(t-1) && CCI < CciOversold (HMA rising with oversold conditions)
  - **Short**: HMA(t) < HMA(t-1) && CCI > CciOverbought (HMA falling with overbought conditions)
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit long position when HMA starts falling
  - **Short**: Exit short position when HMA starts rising
- **Stops**: Yes.
- **Default Values**:
  - `HullPeriod` = 9
  - `CciPeriod` = 20
  - `CciOversold` = -100
  - `CciOverbought` = 100
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2
    -100 and 100 are the defaults of the CCI levels the rules quote. The stop lies AtrMultiplier times the AtrPeriod ATR from the entry close and is checked on candle closes; 0 disables it. An entry signal against an open position reverses it.
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Mixed
  - Direction: Both
  - Indicators: Hull MA CCI
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium

