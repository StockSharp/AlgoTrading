# UltraFATL Threshold Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses the UltraFATL oscillator to detect shifts in trend strength. The indicator outputs discrete levels from 0 to 8. A long position is opened when the previous value is above level 4 and the current value falls below 5 while staying positive. A short position is opened when the previous value is below 5 but above zero and the current value rises above 4. The algorithm works on 4‑hour candles by default but the timeframe can be adjusted.

The approach expects trend continuation after a pullback from extreme UltraFATL readings. Positions are reversed when the opposite condition appears.

## Details

- **Entry Criteria**:
  - **Long**: `UltraFATL(prev) > 4` and `UltraFATL(curr) < 5` and `UltraFATL(curr) != 0`.
  - **Short**: `UltraFATL(prev) < 5` and `UltraFATL(prev) != 0` and `UltraFATL(curr) > 4`.
- **Long/Short**: Both sides.
- **Exit Criteria**: Opposite signal reverses the position.
- **Stops**: Not used by default.
- **Default Values**:
  - `Candle Type` = 4‑hour candles.
  - `Length` = 3.
  - `Signal Bar` = 1 (use previous bar for signals).
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Single (UltraFATL)
  - Stops: No
  - Complexity: Medium
  - Timeframe: Medium-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Moderate
