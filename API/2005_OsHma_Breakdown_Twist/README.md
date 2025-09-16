# OsHMA Breakdown Twist
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy built on the OsHMA oscillator (difference between fast and slow Hull Moving Averages). It can operate in two modes:

- **Breakdown** – trades when the oscillator crosses the zero line.
- **Twist** – trades when the oscillator changes its direction.

The strategy subscribes to candles of a selected timeframe and uses Hull Moving Average indicators to compute the oscillator.

## Details

- **Entry Criteria**: OsHMA zero crossing or direction change.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or stop.
- **Stops**: Take profit and stop loss.
- **Default Values**:
  - `FastHma` = 13
  - `SlowHma` = 26
  - `Mode` = Twist
  - `TakeProfit` = 2000
  - `StopLoss` = 1000
  - `CandleType` = TimeSpan.FromHours(4)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: MA
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: H4
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
