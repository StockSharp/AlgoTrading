# High Low Breakout Statistical Analysis Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Trades breakouts of selected timeframe high or low levels. The strategy can enter long or short based on configured option and closes position after a fixed number of bars.

## Details

- **Entry Criteria**: Close crosses selected high or low level.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or after HoldingPeriod bars.
- **Stops**: No.
- **Default Values**:
  - `EntryOption` = LongAtHigh
  - `TimeframeOption` = Daily
  - `HoldingPeriod` = 5
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: High, Low
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
