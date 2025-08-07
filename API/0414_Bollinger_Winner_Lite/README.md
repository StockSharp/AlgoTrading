# Bollinger Winner Lite
[Русский](README_ru.md) | [中文](README_cn.md)

Bollinger Winner Lite is a stripped‑down reversal system that reacts to price
stretching beyond the Bollinger Bands.  It watches for large candles closing
outside a band and anticipates a quick snap back inside.

The `CandlePercent` parameter defines how big the breakout candle must be
relative to recent moves.  Only candles exceeding this threshold trigger trades,
filtering out small fluctuations.  By default the strategy trades only the long
side, but enabling `ShowShort` allows mirrored short setups.

Exits occur when price touches the opposite band or returns to the middle line.
No hard stop is used; the system relies on mean reversion.

## Details
- **Data**: Price candles.
- **Entry Criteria**:
  - **Long**: Close below lower band with candle size > `CandlePercent`.
  - **Short**: Close above upper band with candle size > `CandlePercent` (requires `ShowShort`).
- **Exit Criteria**: Touch of middle band or opposite band.
- **Stops**: None by default.
- **Default Values**:
  - `BBLength` = 20
  - `BBMultiplier` = 2.0
  - `CandlePercent` = 30
  - `ShowShort` = false
- **Filters**:
  - Category: Mean reversion
  - Direction: Long only by default
  - Indicators: Bollinger Bands
  - Complexity: Simple
  - Risk level: Medium
