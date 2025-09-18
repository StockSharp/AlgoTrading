# AK47 A1
[Русский](README_ru.md) | [中文](README_cn.md)

Port of the "AK47_A1" MetaTrader expert. The strategy combines Bill Williams' Alligator, DeMarker oscillator, Williams %R filter and fractal triggers to trade breakouts only when the market leaves ranging conditions.

## Details
- **Data**: Price candles defined by `CandleType`.
- **Indicators**:
  - Alligator jaw/teeth/lips are 13/8/5 period SMMAs shifted by 8/5/3 bars and fed with median price.
  - DeMarker with period 13 must be on the long side of 0.5 for buys and below 0.5 for sells.
  - Williams %R with period 14 is normalized to `[0;1]`; the previous bar must stay between 0.25 and 0.75 to avoid overbought/oversold states.
  - Fractals are detected from the last 5 highs and lows and remain valid for three bars.
- **Entry Criteria**:
  - All three Alligator lines must be separated by at least `SpanGatorPoints` points (in both bullish and bearish alignment).
  - **Long**: The most recent lower fractal is fresh, DeMarker ≥ 0.5 and the Williams %R filter approves the trade.
  - **Short**: The most recent upper fractal is fresh, DeMarker ≤ 0.5 and the Williams %R filter approves the trade.
  - Opposite positions are flattened before opening a new one.
- **Exit Criteria**:
  - Hard stop-loss and take-profit defined by `StopLossPoints` and `TakeProfitPoints` (converted to absolute prices via the instrument step).
  - Optional trailing stop that trails the close by `TrailingStopPoints` points once the position moves in favor.
  - When a reverse signal appears the current position is closed before opening the new one.
- **Defaults**:
  - `SpanGatorPoints` = 0.5
  - `TakeProfitPoints` = 100
  - `StopLossPoints` = 0 (disabled)
  - `TrailingStopPoints` = 50
  - `CandleType` = 1 hour candles
