# Momentum Long + Short Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This momentum strategy trades both long and short positions on a 3-hour timeframe. Long setups require price to stay above the 100 and 500 period moving averages and can be filtered by RSI, ADX, ATR and trend alignment. Short entries look for price breaking below the lower Bollinger Band while remaining under both averages, with optional ATR confirmation and the ability to block shorts during strong uptrends.

## Details

- **Entry Criteria**:
  - **Long**: price above MA100 and MA500, trend alignment optional, RSI above its smoothed value, ADX above its smoothed value and ATR above its smoothed value.
  - **Short**: price below MA100 and MA500, below lower Bollinger Band, RSI below threshold, ATR above its smoothed value and optional uptrend block.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: stop-loss at `slPercentLong`% below entry; closes early if price drops under MA500.
  - **Short**: stop-loss and take-profit based on percentages `slPercentShort` and `tpPercentShort`.
- **Stops**: Yes.
- **Default Values**:
  - `slPercentLong = 3`
  - `slPercentShort = 3`
  - `tpPercentShort = 4`
  - `rsiLengthLong = 14`
  - `rsiLengthShort = 14`
  - `adxLength = 14`
  - `atrLength = 14`
  - `bbLength = 20`
- **Filters**:
  - Category: Momentum
  - Direction: Both
  - Indicators: Multiple
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Medium-term
