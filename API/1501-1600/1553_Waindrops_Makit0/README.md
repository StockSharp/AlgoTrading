# Waindrops Makit0
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Simplified strategy that compares VWAP of two halves of a custom period.

## Details

- **Entry Criteria**: Right-half VWAP vs left-half VWAP.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal.
- **Stops**: No.
- **Default Values**:
  - `PeriodMinutes` = 60
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Volume
  - Direction: Both
  - Indicators: VWAP
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (1m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Low
