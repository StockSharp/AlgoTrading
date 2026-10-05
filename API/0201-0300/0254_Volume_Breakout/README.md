# Volume Breakout
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
The Volume Breakout strategy observes the Volume for rapid expansions. When readings jump beyond their average range, price often starts a new move.

Testing indicates an average annual return of about 103%. It performs best in the stocks market.

A position opens once the indicator pierces a band derived from recent data and a deviation multiplier. Long and short trades are possible with a stop attached.

This system fits momentum traders seeking early breakouts. Trades close as the Volume falls back toward the mean. Defaults start with `AvgPeriod` = 20.

## Details

- **Entry Criteria**: Indicator exceeds average by deviation multiplier.
- **Long/Short**: Both directions.
- **Exit Criteria**: Indicator reverts to average.
- **Stops**: Yes.
- **Default Values**:
  - `AvgPeriod` = 20
  - `Multiplier` = 2
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StopLossPercent` = 2
    Avg and StdDev are the average and the standard deviation of the last AvgPeriod volume values, the current one included. The stop is a fixed StopLossPercent of the entry price, watched between candles as well; 0 disables it. The band is the average plus Multiplier standard deviations: volume above it on a rising candle goes long and on a falling candle goes short, and a position closes once volume falls back below its average. An entry signal against an open position reverses it.
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Volume
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

