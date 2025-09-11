# RSI Long Only with Confirmed Crossbacks
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy waits for the RSI to drop below a threshold and then cross back above it. The crossback confirms oversold conditions before entering a long position. Positions close when the RSI crosses above an exit level. Parameters allow short trades, but defaults effectively disable shorts.

## Details

- **Entry Criteria**: RSI crosses above the oversold level after being below it.
- **Long/Short**: Long only by default.
- **Exit Criteria**: RSI crosses above the long exit level or optional short rules.
- **Stops**: No.
- **Default Values**:
  - `CandleType` = 5 minute
  - `RsiLength` = 14
  - `Oversold` = 44
  - `LongExitLevel` = 70
  - `ShortEntryLevel` = 100
  - `ShortExitLevel` = 0
- **Filters**:
  - Category: Reversal
  - Direction: Long
  - Indicators: RSI
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
