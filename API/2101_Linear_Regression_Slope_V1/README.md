# Linear Regression Slope V1
[Русский](README_ru.md) | [中文](README_cn.md)

Uses the slope of a linear regression and its shifted copy to detect trend changes.

## Details
- **Data**: Price candles.
- **Entry**:
  - Buy when the slope crosses below its shifted value.
  - Sell when the slope crosses above its shifted value.
- **Exit**: Opposite signal closes the position.
- **Instruments**: Any instruments.
- **Risk**: No built-in stop or target.
