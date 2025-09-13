# Exp TSI CCI Strategy

This strategy computes the True Strength Index (TSI) based on the Commodity Channel Index (CCI) and trades on crossovers with a signal line.

## Logic
- Calculate CCI using the specified period.
- Feed CCI values into the True Strength Index with short and long smoothing lengths.
- Smooth the resulting TSI with an EMA to obtain a signal line.
- Go long when TSI crosses above the signal line.
- Go short when TSI crosses below the signal line.

## Parameters
- `Candle Type` – time frame of candles used for analysis.
- `CCI Period` – period for the Commodity Channel Index.
- `TSI Short Length` – short smoothing length of TSI.
- `TSI Long Length` – long smoothing length of TSI.
- `Signal Length` – EMA length for the TSI signal line.

## Indicators
- Commodity Channel Index
- True Strength Index
- Exponential Moving Average

## Disclaimer
This strategy is provided for educational purposes only and does not constitute investment advice.
