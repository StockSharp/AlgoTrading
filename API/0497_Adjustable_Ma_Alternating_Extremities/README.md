# Adjustable MA & Alternating Extremities Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy uses Bollinger Bands to emulate the Adjustable Moving Average with alternating extremities. A long position is opened when price breaks above the upper band, while a short position is opened when price drops below the lower band. The overshoot state alternates, preventing consecutive trades in the same direction.

## Details

- **Entry Criteria**:
  - Go long when candle high crosses above the upper band.
  - Go short when candle low crosses below the lower band.
- **Exit Criteria**:
  - Opposite band breakout.
- **Indicators**: Bollinger Bands (SMA + standard deviation).
- **Default Values**:
  - Length = 50
  - Multiplier = 2
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Timeframe: Short/medium
  - Risk level: Medium
