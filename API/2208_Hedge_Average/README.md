# Hedge Average Strategy

This strategy reproduces the "Hedge Average" MetaTrader expert. It compares simple moving averages of open and close prices across two time periods.

## Trading Logic

- Calculate SMA of the open and close price for `Period1` and `Period2`.
- If the long-period open average is above its close average **and** the short-period open average is below its close average, a long position is opened.
- If the long-period open average is below its close average **and** the short-period open average is above its close average, a short position is opened.
- Trading is allowed only between `StartHour` and `EndHour`.
- Optional stop-loss and take-profit are set in absolute price units. Trailing stop moves the protective stop along with price when enabled.

## Parameters

- `Period1` – period for the fast averages.
- `Period2` – period for the slow averages.
- `StartHour` – hour of day when trading becomes active.
- `EndHour` – hour of day when trading stops.
- `CandleType` – candle timeframe used for calculations.
- `TakeProfit` – take profit distance in price units.
- `StopLoss` – stop loss distance in price units.
- `UseTrailing` – enable trailing stop based on stop-loss distance.

## Notes

The strategy uses a single position approach and does not replicate money-based profit target from the original MQL version.
