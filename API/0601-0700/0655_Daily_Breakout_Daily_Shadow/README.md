# Daily Breakout Daily Shadow Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy trades daily breakouts using the last two completed daily candles. It closes any open position at the start of each new day.

## Details

- **Entry Criteria**:
  - Long: Previous day closes above the body high of the candle before it and opens below that level.
  - Short: Previous day closes below the body low of the candle before it and opens above that level.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Position is closed at the start of a new day.
- **Stops**: No.
- **Default Values**:
  - `CandleType` = 1 Day
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: None
  - Stops: No
  - Complexity: Low
  - Timeframe: Daily
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
