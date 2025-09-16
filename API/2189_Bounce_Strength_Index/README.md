# Bounce Strength Index Strategy

This strategy implements a simplified version of the Bounce Strength Index (BSI). The indicator measures how price closes within a recent range and applies double smoothing to highlight momentum shifts.

## Logic
- Calculate the recent highest and lowest prices using **Highest** and **Lowest** indicators.
- Determine the close position within that range and smooth the result twice with **SimpleMovingAverage**.
- When the indicator turns upward, short positions are closed and a long position is opened.
- When the indicator turns downward, long positions are closed and a short position is opened.

## Parameters
- `CandleType` – candle series used for analysis.
- `RangePeriod` – lookback period for range calculation.
- `Slowing` – fast smoothing length.
- `AvgPeriod` – slow smoothing length.

## Indicators
- BounceStrengthIndex (custom)
- Highest
- Lowest
- SimpleMovingAverage
