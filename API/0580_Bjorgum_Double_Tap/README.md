# Bjorgum Double Tap Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy searches for double top and double bottom patterns. A short trade is opened when price breaks below the neckline of a double top, and a long trade is opened when price breaks above the neckline of a double bottom. Target and stop levels are calculated as Fibonacci extensions of the pattern height.

## Details

- **Entry Criteria**:
  - **Long**: Double bottom breakout above the neckline.
  - **Short**: Double top breakout below the neckline.
- **Long/Short**: Both.
- **Exit Criteria**: Stop or target levels.
- **Stops**: Fibonacci percentage via `StopLossFib`.
- **Default Values**:
  - Pivot length 50.
  - Pivot tolerance 15%.
  - Target fib 100%.
  - Stop fib 0%.
- **Filters**:
  - Category: Pattern
  - Direction: Both
  - Indicators: Highest/Lowest
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Medium-term
