# Bar Range Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Bar Range strategy enters long when the current bar's range is among the highest of recent bars and the candle closes below its open. The position is closed after a fixed number of bars.

## Details

- **Entry Criteria**:
  - Range = High − Low
  - Percent rank of range over `LookbackPeriod` ≥ `PercentRankThreshold`
  - Close < Open
- **Exit Criteria**: Close position after `ExitBars` bars.
- **Default Values**:
  - `LookbackPeriod` = 50
  - `PercentRankThreshold` = 95
  - `ExitBars` = 1
- **Filters**:
  - Category: Volatility
  - Direction: Long
  - Indicators: Percent Rank
  - Stops: No
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

