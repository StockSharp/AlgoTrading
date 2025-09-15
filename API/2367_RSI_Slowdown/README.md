# RSI Slowdown Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The RSI Slowdown strategy reacts to extreme readings of the Relative Strength Index that show signs of weakening momentum. When RSI approaches overbought or oversold zones and its change between bars drops below one point, the strategy assumes the market is ready for a reversal.

A long position opens when RSI meets or exceeds the upper level and the indicator's growth slows. A short position opens when RSI falls to the lower level with a similar slowdown. Any existing opposite position is closed before entering a new trade.

The default configuration uses 6-hour candles and a 2-period RSI with thresholds of 90 and 10. These values mimic the original MetaTrader implementation.

## Details
- **Entry Criteria**:
  - **Long**: RSI >= `LevelMax` and `|RSI - prev RSI| < 1` (when slowdown is enabled)
  - **Short**: RSI <= `LevelMin` and `|RSI - prev RSI| < 1` (when slowdown is enabled)
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Opposite signal or short entry.
  - **Short**: Opposite signal or long entry.
- **Stops**: No automatic stops.
- **Default Values**:
  - `RsiPeriod` = 2
  - `LevelMax` = 90
  - `LevelMin` = 10
  - `SeekSlowdown` = true
  - `CandleType` = `TimeSpan.FromHours(6)`
- **Filters**:
  - Category: Reversal
  - Direction: Both
  - Indicators: RSI
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday to swing
  - Seasonality: No
  - Neural networks: No
  - Divergence: Yes (slowdown)
  - Risk Level: Medium
