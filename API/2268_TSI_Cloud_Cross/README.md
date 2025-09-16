# TSI Cloud Cross Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The TSI Cloud Cross strategy compares the True Strength Index (TSI) with a delayed copy of itself to form a cloud. A long position is opened when TSI crosses above the shifted line, indicating bullish momentum. A short position is opened when TSI crosses below the shifted line. Signals can be inverted and opposing positions optionally closed.

## Details

- **Entry Criteria**:
  - TSI crosses above its shifted value (long).
  - TSI crosses below its shifted value (short).
- **Long/Short**: Both.
- **Exit Criteria**:
  - Optional closing on opposite signal.
- **Stops**: None.
- **Default Values**:
  - `LongLength` = 25
  - `ShortLength` = 13
  - `TriggerShift` = 1
  - `Invert` = false
- **Filters**:
  - Category: Momentum oscillator
  - Direction: Long/Short
  - Indicators: True Strength Index
  - Stops: No
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
