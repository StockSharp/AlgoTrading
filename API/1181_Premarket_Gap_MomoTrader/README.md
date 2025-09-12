# Premarket Gap MomoTrader Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades a single long breakout during the premarket session when the current candle gains at least a specified percentage above the previous close, prints a bullish candle with sufficient volume, and the candle body occupies a large part of its range. Position size is scaled depending on the body size.

After entry the strategy holds the position while the next candles remain bullish and their volume increases. A red candle or non-increasing volume exits the position. Only one trade is allowed per day and trading can be restricted to the 04:00–09:30 session.

## Details

- **Entry Criteria**:
  - Current candle gain ≥ `MinGainPct` compared to previous close.
  - Candle is green and `Volume` > `MinVolume`.
  - Body percent defines position size: ≥90% → 100%, ≥85% → 50%, ≥75% → 25%.
  - Optional session filter 04:00–09:30 if `UseSession` is enabled.
- **Exit Criteria**:
  - First red candle or candle with non-increasing volume after entry.
- **Stops**: No.
- **Default Values**:
  - `MinGainPct` = 5.
  - `MinVolume` = 15000.
  - `UseSession` = true.
- **Filters**:
  - Category: Momentum
  - Direction: Long
  - Indicators: None
  - Stops: No
  - Complexity: Medium
  - Timeframe: Intraday
