# TTM Grid Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

TTM Grid Strategy builds buy and sell grids based on a simple TTM state derived from EMA of highs and lows. The grid resets when the state changes, and orders are placed whenever price touches a grid level.

## Details

- **Entry Criteria**: Price reaches grid level according to TTM state.
- **Long/Short**: Both directions.
- **Exit Criteria**: None (positions accumulate).
- **Stops**: No.
- **Default Values**:
  - `TtmPeriod` = 6
  - `GridLevels` = 5
  - `GridSpacing` = 0.01m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Grid
  - Direction: Both
  - Indicators: EMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
