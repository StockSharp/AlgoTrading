# VWAP Stdev Bands Strategy (Long Only)

Buys when price crosses below the lower VWAP standard deviation band and closes on reaching the profit target.

## Parameters

- **DevUp**: Standard deviation multiplier above VWAP.
- **DevDown**: Standard deviation multiplier below VWAP.
- **ProfitTarget**: Profit target in price units.
- **GapMinutes**: Gap before new order in minutes.
- **CandleType**: Type of candles.

