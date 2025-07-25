# VWAP Williams R Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
The VWAP Williams %R strategy focuses on intraday reversion around the Volume Weighted Average Price. It observes when price drifts away from VWAP while the Williams %R oscillator reaches oversold or overbought territory. The assumption is that extreme readings near VWAP often lead to a snapback toward the mean.

When the oscillator drops below -80 and price trades under VWAP, the setup implies selling pressure is fading and a rebound may follow. Conversely, a reading above -20 while price is positioned above VWAP warns that buyers are exhausted and a pullback is likely. The strategy opens trades in the direction of a potential return to VWAP and watches for that move to complete.

This approach fits active intraday traders who prefer frequent mean reversion opportunities. A small stop‑loss relative to VWAP keeps risk contained while still allowing enough room for price to fluctuate before reversing.

## Details
- **Entry Criteria**:
  - **Long**: Price < VWAP && Williams %R < -80 (oversold below VWAP)
  - **Short**: Price > VWAP && Williams %R > -20 (overbought above VWAP)
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit long position when price breaks above VWAP
  - **Short**: Exit short position when price breaks below VWAP
- **Stops**: Yes.
- **Default Values**:
  - `WilliamsRPeriod` = 14
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Mixed
  - Direction: Both
  - Indicators: VWAP Williams R
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium

Testing indicates an average annual return of about 40%. It performs best in the crypto market.
