# MTF Seconds Values JD Strategy

This strategy demonstrates handling of custom multi-timeframe candles based on a specified seconds interval. It calculates a simple moving average over aggregated candles and trades on price crossing the average.

## Parameters

- `SecondsTimeframe` – seconds interval for candle aggregation.
- `AverageLength` – period for the simple moving average.
