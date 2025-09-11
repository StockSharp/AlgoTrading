# Triple MA HTF Strategy - Dynamic Smoothing
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that compares three moving averages calculated on higher timeframes.
Each higher timeframe MA is smoothed proportionally to the ratio between its timeframe and the working timeframe.
Signals are generated when the first MA crosses the second while the third confirms the direction.

## Details

- **Entry Criteria**: Cross of MA1 and MA2 with MA3 trend confirmation.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal.
- **Stops**: No.
- **Default Values**:
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `HigherTimeFrame1` = TimeSpan.FromMinutes(15)
  - `HigherTimeFrame2` = TimeSpan.FromMinutes(60)
  - `HigherTimeFrame3` = TimeSpan.FromMinutes(240)
  - `Length1` = 21
  - `Length2` = 21
  - `Length3` = 50
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: MA
  - Stops: None
  - Complexity: Intermediate
  - Timeframe: Intraday (5m base)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
