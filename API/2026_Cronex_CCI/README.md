# Cronex CCI
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy based on Cronex Commodity Channel Index crossover. The indicator smooths the CCI through two exponential moving averages to create a fast and a slow line.

The strategy opens a long position when the fast line crosses below the slow line and closes any short position. A short position is opened when the fast line crosses above the slow line and closes any long position.

This contrarian approach attempts to capture reversals after momentum shifts. It works on higher timeframes such as 4 hour candles.

## Details

- **Entry Criteria**: Crossovers of fast and slow smoothed CCI lines.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite crossover.
- **Stops**: No.
- **Default Values**:
  - `CciPeriod` = 25
  - `FastPeriod` = 14
  - `SlowPeriod` = 25
  - `CandleType` = TimeSpan.FromHours(4)
  - `EnableLongEntry` = true
  - `EnableShortEntry` = true
  - `EnableLongExit` = true
  - `EnableShortExit` = true
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: CCI, EMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Swing (4h)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
