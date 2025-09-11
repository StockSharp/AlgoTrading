# Gann Swing Multi Layer
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy using simplified multi-layer Gann swing analysis.
Trades when three consecutive swing directions align.

The approach follows the classic Gann idea of swing direction changes.
It waits for three consistent swing shifts before entering a position.

## Details

- **Entry Criteria**: Three consecutive swing directions in same orientation.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite swing direction.
- **Stops**: No.
- **Default Values**:
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Swing
  - Direction: Both
  - Indicators: Gann
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (1m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
