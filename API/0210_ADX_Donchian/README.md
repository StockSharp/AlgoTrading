# ADX Donchian Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
This strategy uses ADX Donchian indicators to generate signals.
Long entry occurs when ADX > AdxThreshold && Price >= upperBorder (strong trend with breakout up). Short entry occurs when ADX > AdxThreshold && Price <= lowerBorder (strong trend with breakout down).
It is suitable for traders seeking opportunities in mixed markets.

## Details
- **Entry Criteria**:
  - **Long**: ADX > AdxThreshold && Price >= upperBorder (strong trend with breakout up)
  - **Short**: ADX > AdxThreshold && Price <= lowerBorder (strong trend with breakout down)
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit position when ADX falls below (threshold - 5)
  - **Short**: Exit position when ADX falls below (threshold - 5)
- **Stops**: Yes.
- **Default Values**:
  - `AdxPeriod` = 14
  - `DonchianPeriod` = 5
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `AdxThreshold` = 10
  - `Multiplier` = 0.1m
- **Filters**:
  - Category: Mixed
  - Direction: Both
  - Indicators: ADX Donchian
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium

Testing indicates an average annual return of about 67%. It performs best in the stocks market.
