# Kolier SuperTrend
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy based on Kolier SuperTrend indicator which applies ATR bands to detect trend reversals.

The indicator draws dynamic support and resistance levels derived from ATR. A bullish reversal occurs when price closes above the lower band and the line flips below price. A bearish reversal happens when price closes below the upper band.

By following this adaptive trail, the strategy attempts to ride strong trends while staying protected when momentum fades.

## Details

- **Entry Criteria**: Price crossing the SuperTrend line.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite crossover.
- **Stops**: No.
- **Default Values**:
  - `Period` = 10
  - `Multiplier` = 3.0m
  - `CandleType` = TimeSpan.FromHours(4)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: ATR, SuperTrend
  - Stops: No
  - Complexity: Basic
  - Timeframe: Swing (4h)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
