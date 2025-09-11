# Reversal Catcher Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Reversal Catcher enters when price pushes beyond a Bollinger band and then re-enters while momentum shifts. It relies on a fast and slow EMA to define trend direction and uses RSI crosses of overbought or oversold levels to time entries. Targets and stops are derived from Bollinger band levels and the prior candle's extreme. Positions may optionally close at a specified end-of-day time.

## Details

- **Entry Criteria**: Price re-enters Bollinger Bands with higher high/low structure and RSI crossing extremes.
- **Long/Short**: Both
- **Exit Criteria**: Stop loss, target, or end-of-day flat
- **Stops**: Previous candle extreme
- **Default Values**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 1.5
  - `FastEmaPeriod` = 21
  - `SlowEmaPeriod` = 50
  - `RsiPeriod` = 14
  - `Overbought` = 70
  - `Oversold` = 30
  - `EndOfDay` = 1500
  - `CandleType` = 5 minute
- **Filters**:
  - Category: Reversal
  - Direction: Both
  - Indicators: Bollinger Bands, EMA, RSI
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

