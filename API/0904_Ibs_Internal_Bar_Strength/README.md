# IBS Internal Bar Strength
[Русский](README_ru.md) | [中文](README_cn.md)

IBS Internal Bar Strength is a mean-reversion strategy that uses the previous bar's close within its range to find oversold or overbought conditions. An optional EMA filter aligns trades with the trend and entries can be added only when price moves a minimum percentage from the last entry. Positions exit when IBS crosses the opposite threshold or a maximum holding time is reached.

## Details
- **Data**: Price candles.
- **Entry Criteria**:
  - **Long**: IBS below entry threshold, EMA condition met, and allowed direction.
  - **Short**: IBS above exit threshold, EMA condition met, and allowed direction.
- **Exit Criteria**: IBS crossing opposite threshold or trade duration limit.
- **Stops**: Time-based exit.
- **Default Values**:
  - `IbsEntryThreshold` = 0.09
  - `IbsExitThreshold` = 0.985
  - `EmaPeriod` = 220
  - `MinEntryPct` = 0
  - `MaxTradeDuration` = 14
- **Filters**:
  - Category: Mean reversion
  - Direction: Long & Short
  - Indicators: IBS, EMA
  - Complexity: Low
  - Risk level: Medium
