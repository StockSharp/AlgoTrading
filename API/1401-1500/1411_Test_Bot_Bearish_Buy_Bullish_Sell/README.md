# Test Bot: Bearish Buy / Bullish Sell
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Enters long on the first bearish candle and exits on the first bullish candle.

## Details

- **Entry Criteria**: First bearish candle when flat.
- **Long/Short**: Long only.
- **Exit Criteria**: First bullish candle.
- **Stops**: No.
- **Default Values**:
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Reversal
  - Direction: Long
  - Indicators: None
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
