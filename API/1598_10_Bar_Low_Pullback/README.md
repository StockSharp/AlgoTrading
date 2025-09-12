# Short Only 10 Bar Low Pullback Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy enters short when price breaks the lowest low of the previous bars and the internal bar strength is above a threshold. An optional EMA filter confirms the downtrend.

## Details

- **Entry Criteria**:
  - Low breaks the previous `LowestPeriod` bars' lowest low.
  - IBS > `IbsThreshold`.
  - Optional: close price below EMA when the filter is enabled.
  - Time within `StartTime` and `EndTime`.
- **Long/Short**: Short only.
- **Exit Criteria**:
  - Close price below previous low closes the short.
- **Stops**: None.
- **Default Values**:
  - `LowestPeriod` = 10
  - `IbsThreshold` = 0.85
  - `UseEmaFilter` = true
  - `EmaPeriod` = 200
- **Filters**:
  - Category: Pullback
  - Direction: Short
  - Indicators: Lowest, EMA
  - Stops: No
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
