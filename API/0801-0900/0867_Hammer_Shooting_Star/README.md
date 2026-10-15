# Hammer & Shooting Star Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy trades hammer and shooting star candlestick patterns.
A long position is opened when the previous candle is a hammer,
while a short position follows a shooting star.
Exits use the signal candle's high and low for take profit and stop loss.

## Details

- **Entry Criteria**:
  - Long: previous candle is a hammer
  - Short: previous candle is a shooting star
- **Long/Short**: Both
- **Exit Criteria**: Stop loss and take profit at previous candle low/high
- **Stops**: Yes, fixed at signal candle extremes
- **Default Values**:
  - `WickFactor` = 0.9
  - `MaxOppositeWickFactor` = 0.45
  - `MinBodyRangePct` = 0.2
  - `CandleType` = 1 minute
- **Filters**:
  - Category: Pattern
  - Direction: Both
  - Indicators: Candlestick
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
