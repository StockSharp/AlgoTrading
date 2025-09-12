# Momentum Alligator 4h Bitcoin Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Momentum Alligator 4h Bitcoin strategy combines the Awesome Oscillator with the Bill Williams Alligator on the daily timeframe. A long position is opened when the oscillator crosses above its 5-period SMA and price trades above all three daily Alligator lines. A dynamic stop-loss uses the greater of a percentage drop from entry and the Alligator jaw line. After a profitable exit the strategy skips the next two signals.

## Details

- **Entry Criteria**: AO crosses above its 5-period SMA and close is above daily Alligator lines.
- **Long/Short**: Long only.
- **Exit Criteria**: Dynamic stop-loss at max of percent stop and Alligator jaw.
- **Stops**: Yes.
- **Default Values**:
  - `StopLossPercent` = 0.02m
  - `CandleType` = TimeSpan.FromHours(4)
  - `TradeStart` = 2023-01-01
  - `TradeStop` = 2025-01-01
- **Filters**:
  - Category: Momentum
  - Direction: Long
  - Indicators: Awesome Oscillator, Alligator
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
