# Mean Reversion Pro Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Mean Reversion Pro is a mean reversion system built for major indices. It uses two moving averages and intrabar range levels to detect pullbacks. Long trades are preferred as indices tend to move upward.

## Details

- **Entry Criteria**:
  - **Long**: Close below fast SMA, close below 20% range level, close above slow SMA, no position.
  - **Short**: Close above fast SMA, close above 80% range level, close below slow SMA, no position.
- **Long/Short**: Both (long recommended).
- **Exit Criteria**:
  - **Long**: Close crosses above fast SMA.
  - **Short**: Close crosses below fast SMA.
- **Stops**: None.
- **Default Values**:
  - `Fast SMA` = 5
  - `Slow SMA` = 100
  - `Direction` = Long only
- **Filters**:
  - Category: Mean reversion
  - Direction: Configurable
  - Indicators: SMA
  - Stops: None
  - Complexity: Simple
  - Timeframe: Daily
