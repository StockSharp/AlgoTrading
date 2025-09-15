# Candels High Open Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that trades when a candle opens exactly at its high or low.
A long position is opened if the candle's open equals its low, anticipating upward movement.
A short position is opened if the candle's open equals its high, expecting a decline.
The position is closed when price crosses the Parabolic SAR value, acting as a trailing exit.

## Details

- **Entry Criteria**:
  - Long: `Open == Low`
  - Short: `Open == High`
- **Long/Short**: Both
- **Exit Criteria**: Price crosses Parabolic SAR or opposite signal
- **Stops**: Uses fixed stop loss and take profit levels
- **Default Values**:
  - `StopLevel` = 50m
  - `TakeLevel` = 50m
  - `SarStep` = 0.02m
  - `SarMax` = 0.2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `ReverseSignals` = false
- **Filters**:
  - Category: Price action
  - Direction: Both
  - Indicators: Parabolic SAR
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
