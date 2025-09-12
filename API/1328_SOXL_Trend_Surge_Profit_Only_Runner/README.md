# SOXL Trend Surge Profit-Only Runner Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy enters long trades when price trends above the 200 EMA and SuperTrend is bullish. It requires rising ATR, volume above average, a session filter, and price to stay outside a small EMA buffer. The system takes partial profit at an ATR-based target and trails the remaining position with an ATR stop.

## Details

- **Entry Criteria**: price above EMA, SuperTrend up, volume above average, ATR rising, outside EMA buffer, time between 14–19 hours, cooldown after exits
- **Long/Short**: Long only
- **Exit Criteria**: 50% partial take profit at ATR target and trailing stop on remainder
- **Stops**: Trailing stop
- **Default Values**:
  - `EmaLength` = 200
  - `AtrLength` = 14
  - `AtrMultTarget` = 2.0
  - `CooldownBars` = 15
  - `SupertrendFactor` = 3.0
  - `SupertrendAtrPeriod` = 10
  - `MinBarsHeld` = 2
  - `VolFilterLen` = 20
  - `EmaBuffer` = 0.005
- **Filters**:
  - Category: Trend
  - Direction: Long
  - Indicators: EMA, ATR, SuperTrend, Volume
  - Stops: Trailing
  - Complexity: Moderate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
