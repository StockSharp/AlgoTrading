# Jpalonso Modoki Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Jpalonso Modoki strategy trades a price channel built from a simple moving average.
Upper and lower envelopes are calculated by applying a percentage deviation to the moving average.
The system goes long when price touches the lower band or when it stays in the upper half of the channel.
It goes short in the opposite situations. Fixed take profit and stop loss protect the position.

## Details

- **Entry Criteria**: Price below lower envelope or between middle and upper band for longs; price above upper envelope or between middle and lower band for shorts.
- **Long/Short**: Both.
- **Exit Criteria**: Opposite signal or stop levels.
- **Stops**: Yes, take profit and stop loss in points.
- **Default Values**:
  - `CandleType` = 1 minute
  - `SmaPeriod` = 200
  - `Deviation` = 0.35%
  - `TakeProfit` = 127 points
  - `StopLoss` = 77 points
- **Filters**:
  - Category: Channel
  - Direction: Both
  - Indicators: SMA, Envelopes
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

