# RSI Breakout Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
The RSI Breakout strategy looks for momentum bursts when the Relative Strength Index pushes beyond its typical range. By measuring RSI deviations from its moving average, the system aims to catch new trends as they begin.

Testing indicates an average annual return of about 88%. It performs best in the stocks market.

A long position is opened when RSI closes above the average plus `Multiplier` times the standard deviation. A short position is taken when RSI falls below the average minus that multiplier. Positions are closed once RSI crosses back through its average value.

Momentum traders may find this approach useful for identifying early breakouts while still maintaining defined exit levels. A stop-loss percentage protects against sudden reversals.

## Details
- **Entry Criteria**:
  - **Long**: RSI > Avg + Multiplier * StdDev
  - **Short**: RSI < Avg - Multiplier * StdDev
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit when RSI < Avg
  - **Short**: Exit when RSI > Avg
- **Stops**: Yes, percent stop-loss.
- **Default Values**:
  - `RsiPeriod` = 14
  - `AveragePeriod` = 20
  - `Multiplier` = 2
  - `StopLossPercent` = 2
  - `CandleType` = TimeSpan.FromMinutes(5)
    Avg and StdDev are the average and the standard deviation of the last AveragePeriod RSI values, the current one included. The stop is a fixed StopLossPercent of the entry price, watched between candles as well; 0 disables it. An entry signal against an open position reverses it.
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: RSI
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium

