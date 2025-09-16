# DiNapoli Stochastic Strategy

This strategy implements a trading system based on the **DiNapoli Stochastic** oscillator. It reacts to the crossovers between the %K and %D lines of the stochastic indicator.

## Strategy Logic

1. Subscribe to candles of the selected timeframe.
2. Calculate the DiNapoli Stochastic values using the standard Stochastic oscillator with smoothing periods.
3. Close short positions whenever the previous %K was above %D.
4. Close long positions whenever the previous %K was below %D.
5. Open a long position when %K crosses below %D and long trades are allowed.
6. Open a short position when %K crosses above %D and short trades are allowed.

## Parameters

- `FastK` – base period for %K.
- `SlowK` – smoothing period for %K.
- `SlowD` – smoothing period for %D.
- `BuyOpen` – enable or disable long entries.
- `SellOpen` – enable or disable short entries.
- `BuyClose` – enable or disable closing long positions.
- `SellClose` – enable or disable closing short positions.
- `CandleType` – candle timeframe used for calculations.

## Notes

The strategy uses the high-level StockSharp API and processes only finished candles. Indicator values are obtained through `BindEx` without using historical value requests.
