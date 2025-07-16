# RSI Overbought/Oversold
[Русский](README_ru.md) | [中文](README_cn.md)
 
This system trades reversals using the Relative Strength Index. When RSI drops below the oversold level, it buys after closing any shorts. When RSI climbs above the overbought level, it sells after closing longs.

Positions exit when RSI returns to a neutral zone or the stop-loss is reached.

## Details

- **Entry Criteria**: RSI below `OversoldLevel` or above `OverboughtLevel`.
- **Long/Short**: Both directions.
- **Exit Criteria**: RSI crosses `NeutralLevel` or stop.
- **Stops**: Yes.
- **Default Values**:
  - `RsiPeriod` = 14
  - `OverboughtLevel` = 70
  - `OversoldLevel` = 30
  - `NeutralLevel` = 50
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StopLossPercent` = 2.0m
- **Filters**:
  - Category: Oscillator
  - Direction: Both
  - Indicators: RSI
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: Yes
  - Risk Level: Medium
