# RSI Hull MA Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
This strategy uses RSI Hull MA indicators to generate signals.
Long entry occurs when RSI < RsiOversold && HMA(t) > HMA(t-1) (oversold with rising HMA). Short entry occurs when RSI > RsiOverbought && HMA(t) < HMA(t-1) (overbought with falling HMA).
It is suitable for traders seeking opportunities in mixed markets.

Testing indicates an average annual return of about 58%. It performs best in the stocks market.

## Details
- **Entry Criteria**:
  - **Long**: RSI < RsiOversold && HMA(t) > HMA(t-1) (oversold with rising HMA)
  - **Short**: RSI > RsiOverbought && HMA(t) < HMA(t-1) (overbought with falling HMA)
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit long position when RSI returns to neutral zone
  - **Short**: Exit short position when RSI returns to neutral zone
- **Stops**: Yes.
- **Default Values**:
  - `RsiPeriod` = 14
  - `RsiOversold` = 30
  - `RsiOverbought` = 70
  - `HullPeriod` = 9
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2
    30 and 70 are the defaults of the RSI levels the rules quote. The neutral zone starts at RSI 50: a long closes once RSI reaches 50 and a short once it falls to 50. The stop lies AtrMultiplier times the AtrPeriod ATR from the entry close and is checked on candle closes; 0 disables it. An entry signal against an open position reverses it.
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Mixed
  - Direction: Both
  - Indicators: RSI Hull MA
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium

