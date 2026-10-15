# Crypto Volatility Bitcoin Correlation
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy enters a long position when Bitcoin volatility rises together with the BVOL7D index and price trades above its EMA. It exits when price drops back below the EMA.

## Details

- **Entry Criteria**: VIXFix greater than previous value, BVOL7D greater than previous value, close above EMA.
- **Long/Short**: Long only.
- **Exit Criteria**: Close below EMA.
- **Stops**: No.
- **Default Values**:
  - `VixFixLength` = 22
  - `EmaLength` = 50
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Volatility
  - Direction: Long
  - Indicators: Highest, EMA
  - Stops: No
  - Complexity: Beginner
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
