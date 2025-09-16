# Forex Fraus Slogger Strategy

This strategy replicates the MetaTrader envelope reversal system.

## Logic

- Calculate a 1-period SMA as the base price.
- Upper and lower envelopes are set at `EnvelopePercent` percent from the base.
- When price closes above the upper band and then returns below, enter a short position.
- When price closes below the lower band and then returns above, enter a long position.
- Positions are protected by a trailing stop.

## Parameters

- `EnvelopePercent` – percentage offset for envelopes (default 0.1).
- `TrailingStop` – trailing stop distance in price units (default 0.001).
- `TrailingStep` – minimum price move required to advance the trailing stop (default 0.0001).
- `ProfitTrailing` – enable trailing only after position becomes profitable.
- `UseTimeFilter` – trade only during specified hours.
- `StartHour` – start of the trading window.
- `StopHour` – end of the trading window.
- `CandleType` – candle timeframe used for calculations.

## Notes

- The strategy uses market orders via `BuyMarket` and `SellMarket`.
- The trailing stop exits the position when price crosses the stop level.
