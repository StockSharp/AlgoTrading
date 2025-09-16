# FRASMAv2
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy based on Fractal Adaptive Simple Moving Average (FRASMAv2).

This strategy calculates a Fractal Adaptive Simple Moving Average using the Fractal Dimension indicator. The indicator color changes depending on slope: green for rising, gray for flat, magenta for falling. The strategy watches color transitions on the last closed candle:

- If the indicator was green on the previous bar and becomes not green (gray or magenta) on the last bar, the strategy closes short positions and opens a new long position.
- If the indicator was magenta and becomes not magenta, the strategy closes long positions and opens a new short position.

Risk management uses stop-loss and take-profit parameters specified in points.

## Details

- **Entry Criteria**: Color changes of FRASMAv2.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite color transition.
- **Stops**: Take profit and stop loss via protection module.
- **Default Values**:
  - `Period` = 30
  - `TakeProfit` = 2000 points
  - `StopLoss` = 1000 points
  - `CandleType` = TimeSpan.FromHours(4)
- **Filters**:
  - Category: Trend Reversal
  - Direction: Both
  - Indicators: FractalDimension, FRASMAv2
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: 4h
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
