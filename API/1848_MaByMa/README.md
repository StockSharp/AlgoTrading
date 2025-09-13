# MA by MA Crossover Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy implements a double smoothed moving average crossover.
The price series is smoothed by a fast exponential moving average (EMA).
The result of the fast EMA is then smoothed again by a slower EMA.
The two series are compared to generate signals:
- A long position is opened when the fast EMA crosses above the slow EMA.
- A short position is opened when the fast EMA crosses below the slow EMA.
Any existing opposite position is closed on crossover.

The strategy works on any timeframe of candlesticks.

## Parameters
- `FastLength` – period of the fast EMA.
- `SlowLength` – period of the slow EMA applied to the fast EMA output.
- `EnableLong` – allow opening long positions.
- `EnableShort` – allow opening short positions.
- `CandleType` – type of candles used for calculations.

## Details
- **Entry Criteria**:
  - **Long**: fast EMA crosses above slow EMA.
  - **Short**: fast EMA crosses below slow EMA.
- **Long/Short**: Both directions supported.
- **Exit Criteria**:
  - Opposite crossover closes an existing position.
- **Stops**: No explicit stop-loss or take-profit is used.
- **Default Values**:
  - `FastLength` = 7
  - `SlowLength` = 7
  - `EnableLong` = true
  - `EnableShort` = true
  - `CandleType` = 12-hour timeframe
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Moving averages
  - Stops: No
  - Complexity: Basic
  - Timeframe: Any
