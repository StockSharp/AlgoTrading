# RSI Hull MA Strategy

This strategy uses RSI Hull MA indicators to generate signals.
Long entry occurs when RSI < 30 && HMA(t) > HMA(t-1) (oversold with rising HMA). Short entry occurs when RSI > 70 && HMA(t) < HMA(t-1) (overbought with falling HMA).
It is suitable for traders seeking opportunities in mixed markets.

## Details
- **Entry Criteria**:
  - **Long**: RSI < 30 && HMA(t) > HMA(t-1) (oversold with rising HMA)
  - **Short**: RSI > 70 && HMA(t) < HMA(t-1) (overbought with falling HMA)
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit long position when RSI returns to neutral zone
  - **Short**: Exit short position when RSI returns to neutral zone
- **Stops**: Yes.
- **Default Values**:
  - `RsiPeriod` = 14
  - `HullPeriod` = 9
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2m
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
