# Hull MA CCI Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
This strategy uses Hull MA CCI indicators to generate signals.
Long entry occurs when HMA(t) > HMA(t-1) && CCI < -100 (HMA rising with oversold conditions). Short entry occurs when HMA(t) < HMA(t-1) && CCI > 100 (HMA falling with overbought conditions).
It is suitable for traders seeking opportunities in mixed markets.

Testing indicates an average annual return of about 52%. It performs best in the crypto market.

## Details
- **Entry Criteria**:
  - **Long**: HMA(t) > HMA(t-1) && CCI < -100 (HMA rising with oversold conditions)
  - **Short**: HMA(t) < HMA(t-1) && CCI > 100 (HMA falling with overbought conditions)
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit long position when HMA starts falling
  - **Short**: Exit short position when HMA starts rising
- **Stops**: Yes.
- **Default Values**:
  - `HullPeriod` = 9
  - `CciPeriod` = 20
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2m
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

