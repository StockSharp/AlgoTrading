# Marneni Money Tree Strategy

This strategy translates the MQL expert advisor "Marneni Money Tree" into StockSharp.
It relies on a 40-period simple moving average (SMA) and two shifted values to detect trend direction.
When the SMA shifted by four bars lies between the current SMA and the value thirty bars ago,
- a market order is sent in the detected direction;
- eight additional limit orders are placed at increasing distances, defined by `Order2Pips` through `Order9Pips`.

Long setups place buy limits below the current price. Short setups place sell limits above the price.
Positions are closed and remaining orders cancelled when the SMA relationship reverses.

## Parameters
- `Order2Pips`–`Order9Pips` — distance in pips for limit orders 2 through 9.
- `CandleType` — timeframe used for calculations.

The base trade volume is fixed at 2 and can be adjusted by changing the `Volume` property before starting the strategy.
