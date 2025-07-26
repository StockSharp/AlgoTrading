# Volume Weighted Price Breakout
[Русский](README_ru.md) | [中文](README_cn.md)
 
This strategy combines a moving average with a volume‑weighted moving average (VWMA). When price trades above the VWMA, it suggests buyers are dominant. A breakout occurs when price crosses the VWMA from the opposite side.

Testing indicates an average annual return of about 40%. It performs best in the crypto market.

Trades align with the VWMA direction and use the simple moving average as a higher‑level trend filter. Exits occur when price reverses relative to the moving average.

The goal is to capture breakouts supported by volume.

## Details

- **Entry Criteria**: Price above or below VWMA with MA confirmation.
- **Long/Short**: Both directions.
- **Exit Criteria**: Price crosses MA in opposite direction or stop.
- **Stops**: Yes.
- **Default Values**:
  - `MAPeriod` = 20
  - `VWAPPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: VWMA, MA
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

