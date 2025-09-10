# Average High-Low Range IBS Reversal Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy seeks mean reversion after price has stayed below a dynamic threshold derived from the average high-low range. It calculates the moving average of the bar range, the highest high and lowest low over the lookback period. A buy threshold is defined as the highest high minus 2.5 times the average range. When price remains below this level for a specified number of bars and the intrabar strength (IBS) is under a given limit within the trading window, a long position is opened. The position is closed if the close exceeds the previous bar's high.

## Details

- **Entry Criteria**:
  - Price has stayed below the buy threshold for `BarsBelowThreshold` bars.
  - IBS < `IbsBuyThreshold`.
  - Time between `StartTime` and `EndTime`.
- **Long/Short**: Long only.
- **Exit Criteria**:
  - Close price exceeds previous bar high.
- **Stops**: None.
- **Default Values**:
  - `Length` = 20
  - `BarsBelowThreshold` = 2
  - `IbsBuyThreshold` = 0.2
- **Filters**:
  - Category: Mean reversion
  - Direction: Long
  - Indicators: SMA, Highest, Lowest
  - Stops: No
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
