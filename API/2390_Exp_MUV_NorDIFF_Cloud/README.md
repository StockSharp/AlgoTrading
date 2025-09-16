# Exp MUV NorDIFF Cloud

Strategy based on normalized momentum of SMA and EMA.
Enters long when SMA or EMA momentum reaches +100 and short when it reaches -100.

## Parameters
- `MaPeriod` – moving average period.
- `MomentumPeriod` – number of bars used for momentum calculation.
- `KPeriod` – window for normalization of momentum extremes.
- `CandleType` – timeframe of candles.

## Notes
The strategy calculates SMA and EMA values, measures their momentum and
normalizes it within the recent range to generate trading signals.
