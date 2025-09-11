# VWAP Mean Magnet v9 (Simple Alert) Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This simplified version of the VWAP Mean Magnet strategy uses VWAP and RSI without volume filtering. Trades open when price deviates from VWAP and RSI reaches extreme levels. Positions are closed when price reverts back to VWAP.

## Details

- **Entry Criteria**:
  - **Long**: price < VWAP and RSI < oversold.
  - **Short**: price > VWAP and RSI > overbought.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Close position when price returns to VWAP.
- **Stops**: Yes, percent stop-loss.
- **Default Values**:
  - `VWAP length` = 60
  - `RSI length` = 14
  - `RSI overbought` = 65
  - `RSI oversold` = 25
  - `Stop loss %` = 0.5
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: Multiple
  - Stops: Yes
  - Complexity: Simple
  - Timeframe: Intraday
