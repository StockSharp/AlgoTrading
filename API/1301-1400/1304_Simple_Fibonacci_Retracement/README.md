# Simple Fibonacci Retracement Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Uses Fibonacci retracement levels derived from the highest high and lowest low over a lookback window. When price crosses a selected Fibonacci level, the strategy enters a position and places fixed pip-based take profit and stop loss orders.

## Details

- **Entry**: Cross above or below the chosen Fibonacci level.
- **Exit**: Fixed take profit or stop loss.
- **Indicators**: Highest, Lowest.
- **Stops**: Yes.
- **Default Values**:
  - `LookbackPeriod` = 100
  - `TakeProfitPips` = 50
  - `StopLossPips` = 20
- **Direction**: Both.
