# TSI DeMarker Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that calculates the True Strength Index on top of the DeMarker oscillator.
A long position is opened when the TSI crosses above its moving average signal line.
A short position is opened when the TSI crosses below the signal line.

The approach combines momentum and overbought/oversold analysis.

## Details

- **Entry Criteria**:
  - Long: `TSI crosses above signal`
  - Short: `TSI crosses below signal`
- **Long/Short**: Both
- **Exit Criteria**: Opposite signal
- **Stops**: No
- **Default Values**:
  - `CandleType` = TimeSpan.FromHours(8).TimeFrame()
  - `DemarkerPeriod` = 25
  - `ShortLength` = 5
  - `LongLength` = 8
  - `SignalLength` = 20
- **Filters**:
  - Category: Oscillator crossover
  - Direction: Both
  - Indicators: TSI, DeMarker
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Medium-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
