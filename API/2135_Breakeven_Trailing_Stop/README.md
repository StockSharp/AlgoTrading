# Breakeven Trailing Stop Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that demonstrates how to move the stop-loss to breakeven and then trail it as price advances.
The strategy enters a long position and manages it in two phases:
1. After the price gains `BreakevenPlus` points, the stop is moved to `BreakevenStep` points beyond the entry.
2. When price continues in profit by `TrailingPlus` points above the stop, the stop trails the price by `TrailingStep` points.

The logic is symmetric for short positions if one is opened manually.

## Details

- **Entry Criteria**: Opens a long position on the first finished candle.
- **Long/Short**: Both (example uses long).
- **Exit Criteria**: Price crosses trailing stop.
- **Stops**: Breakeven and trailing stop.
- **Default Values**:
  - `BreakevenPlus` = 5
  - `BreakevenStep` = 2
  - `TrailingPlus` = 3
  - `TrailingStep` = 1
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filters**:
  - Category: Stop management
  - Direction: Both
  - Indicators: None
  - Stops: Breakeven, trailing
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
