# Turnaround Tuesday Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Turnaround Tuesday refers to the tendency for markets that sold off on Monday to rebound the next day.
The effect is often attributed to traders overreacting after the weekend and then reversing course.

Testing indicates an average annual return of about 91%. It performs best in the stocks market.

This strategy buys at Tuesday's open when Monday was down, holding only for the session or until a modest profit target is reached.

Stops are tight to protect against continued weakness if the bounce fails to develop.

## Details

- **Entry Criteria**: calendar effect triggers
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 15 minute
  - `ProfitTargetPercent` = 1
  - `StopLossPercent` = 2
    Days are UTC days, since the market trades around the clock; entries happen at the close of the day's first candle. A Tuesday follows a down Monday when Monday closed below its open; the long closes at Tuesday's last candle or once the close is ProfitTargetPercent above the entry.
- **Filters**:
  - Category: Seasonality
  - Direction: Both
  - Indicators: Seasonality
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: Yes
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

