# Close Positions Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Closes open positions based on profit, loss or time rules. No new orders are opened by this strategy.

## Details

- **Entry Criteria**: none, positions are assumed to be opened externally.
- **Exit Criteria**:
  - Reached profit or loss limit measured in pips.
  - Position age exceeds time limit in minutes.
  - Current time is after configured close time.
- **Stops**: implicit profit and loss thresholds.
- **Filters**: time of day and holding time.
