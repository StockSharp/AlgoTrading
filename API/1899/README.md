# Adaptive Renko Strategy

This strategy builds an adaptive Renko grid where the brick size follows market volatility measured by the **Average True Range (ATR)** indicator. A trade is executed whenever price travels a full brick in either direction.

## Logic
- ATR is calculated over a configurable `VolatilityPeriod`.
- The brick size equals `ATR * Multiplier` but cannot be less than `MinBrickSize`.
- When price rises above the previous brick by at least one brick size, the strategy buys (closing short positions if needed).
- When price falls below the previous brick by at least one brick size, the strategy sells (closing long positions if needed).

## Parameters
- `Volume` – order volume.
- `VolatilityPeriod` – period used for ATR.
- `Multiplier` – coefficient applied to ATR.
- `MinBrickSize` – minimal allowed brick size in price units.
- `CandleType` – timeframe for ATR calculation.

## Timeframe
- Default: 4 hours.
