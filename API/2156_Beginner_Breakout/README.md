# Beginner Breakout Strategy

Uses the highest and lowest prices of the recent `Period` candles to form a channel. When the close approaches the upper boundary, the strategy goes long. When the close approaches the lower boundary, it goes short.

## Entry Rules
- **Long**: Close >= highest - (highest - lowest) * `ShiftPercent` / 100 and trend is not already up.
- **Short**: Close <= lowest + (highest - lowest) * `ShiftPercent` / 100 and trend is not already down.

## Exit Rules
- Opposite signal closes the current position and opens a new one in the other direction.

## Parameters
- `Period` – bars to look back for channel calculation.
- `ShiftPercent` – percentage offset from channel borders.
- `CandleType` – timeframe of working candles.
- `Volume` – trade volume.
- `StopLoss` – stop loss in price units.
- `TakeProfit` – take profit in price units.

## Indicators
- Highest
- Lowest
