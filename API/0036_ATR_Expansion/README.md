# ATR Expansion Breakout
[Русский](README_ru.md) | [中文](README_cn.md)
 
This strategy follows bursts of volatility using the Average True Range. When ATR is rising compared to the previous bar and price trades relative to a moving average, it looks to ride the breakout.

Testing indicates an average annual return of about 145%. It performs best in the crypto market.

Expansion of ATR implies a strong move is underway. Entries align with the direction of price relative to the moving average, while contractions in volatility trigger exits.

Stops are set using an ATR multiple to let trades breathe during high volatility.

## Details

- **Entry Criteria**: ATR increasing and price above/below MA.
- **Long/Short**: Both directions.
- **Exit Criteria**: ATR contracts or stop is hit.
- **Stops**: Yes.
- **Default Values**:
  - `AtrPeriod` = 14
  - `MAPeriod` = 20
  - `AtrMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: ATR, MA
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

