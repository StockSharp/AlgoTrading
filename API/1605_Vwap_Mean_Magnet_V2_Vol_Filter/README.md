# VWAP Mean Magnet v2 (Volume Filter) Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy combines a VWAP mean-reversion concept with RSI and a volume filter. Trades are taken when price deviates from VWAP and RSI reaches extreme levels, provided the current volume is above a moving average multiplied by a factor.

## Details

- **Entry Criteria**:
  - **Long**: price < VWAP, RSI < oversold, volume filter passes.
  - **Short**: price > VWAP, RSI > overbought, volume filter passes.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Close position when price returns to VWAP.
- **Stops**: Yes, percent stop-loss.
- **Default Values**:
  - `VWAP length` = 60
  - `RSI length` = 14
  - `RSI overbought` = 65
  - `RSI oversold` = 25
  - `Volume lookback` = 20
  - `Volume multiplier` = 3
  - `Stop loss %` = 0.5
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: Multiple
  - Stops: Yes
  - Complexity: Moderate
  - Timeframe: Intraday
