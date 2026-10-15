# VWAP Williams R Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
The VWAP Williams %R strategy focuses on intraday reversion around the Volume Weighted Average Price. It observes when price drifts away from VWAP while the Williams %R oscillator reaches oversold or overbought territory. The assumption is that extreme readings near VWAP often lead to a snapback toward the mean.

When the oscillator drops below -80 and price trades under VWAP, the setup implies selling pressure is fading and a rebound may follow. Conversely, a reading above -20 while price is positioned above VWAP warns that buyers are exhausted and a pullback is likely. The strategy opens trades in the direction of a potential return to VWAP and watches for that move to complete.

This approach fits active intraday traders who prefer mean-reversion opportunities. A percentage stop-loss measured from each fill limits adverse movement while the VWAP exit captures the intended return to the mean.

## Details
- **Entry Criteria**:
  - **Long**: the close is at least 0.1% below the session VWAP and Williams %R crosses down through -80.
  - **Short**: the close is at least 0.1% above the session VWAP and Williams %R crosses up through -20.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit long position when price breaks above VWAP
  - **Short**: Exit short position when price breaks below VWAP
- **Stops**: Yes.
- **Default Values**:
  - `WilliamsRPeriod` = 14
  - `CooldownBars` = 60
  - `StopLossPercent` = 2%
  - `CandleType` = TimeSpan.FromMinutes(30)
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

