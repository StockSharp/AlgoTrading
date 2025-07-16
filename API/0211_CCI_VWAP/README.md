# CCI VWAP Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
The CCI VWAP approach attempts to capture intraday reversals when momentum and price align away from the volume weighted average price. By observing the Commodity Channel Index alongside the VWAP level, the system measures the strength of recent swings relative to a fair value benchmark.

A buy setup emerges when the CCI falls below -100 and the market trades beneath VWAP, signalling that selling pressure may be exhausted. A short occurs when the CCI rises above +100 with price above VWAP, highlighting a stretched rally vulnerable to a setback. Positions are closed once price reclaims the VWAP in the opposite direction.

This strategy is designed for day traders who like to fade extremes yet still rely on objective levels for exits. The defined stop-loss helps manage risk if momentum does not quickly mean revert.

## Details
- **Entry Criteria**:
  - **Long**: CCI < -100 && Price < VWAP (oversold below VWAP)
  - **Short**: CCI > 100 && Price > VWAP (overbought above VWAP)
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit long when price moves above VWAP
  - **Short**: Exit short when price falls below VWAP
- **Stops**: Yes.
- **Default Values**:
  - `CciPeriod` = 20
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Mixed
  - Direction: Both
  - Indicators: CCI VWAP
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium
