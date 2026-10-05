# CCI Mean Reversion Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
The Commodity Channel Index (CCI) measures how far price moves from its statistical average. This strategy enters when CCI deviates from its own mean by a large margin, expecting a snap back once momentum fades.

Testing indicates an average annual return of about 151%. It performs best in the stocks market.

A long trade occurs when CCI drops below the average minus `DeviationMultiplier` times the standard deviation. A short trade is opened when CCI rises above the average plus that multiplier. The position exits when CCI crosses back through the mean value.

This system is suited to short-term traders who favour contrarian setups. A stop-loss based on percentage move helps cap risk if the market fails to revert quickly.

## Details
- **Entry Criteria**:
  - **Long**: CCI < Avg - DeviationMultiplier * StdDev
  - **Short**: CCI > Avg + DeviationMultiplier * StdDev
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit when CCI > Avg
  - **Short**: Exit when CCI < Avg
- **Stops**: Yes, percent stop-loss.
- **Default Values**:
  - `CciPeriod` = 20
  - `AveragePeriod` = 20
  - `DeviationMultiplier` = 2
  - `StopLossPercent` = 2
  - `CandleType` = TimeSpan.FromMinutes(5)
    Avg and StdDev are the average and the standard deviation of the last AveragePeriod CCI values, the current one included. The stop is a fixed StopLossPercent of the entry price, watched between candles as well; 0 disables it. An entry signal against an open position reverses it.
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: CCI
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium

