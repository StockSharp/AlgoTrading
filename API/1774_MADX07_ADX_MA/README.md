# MADX-07 ADX MA Strategy

This strategy is converted from the MADX-07 MQL4 expert advisor. It trades on H4 candles and combines two moving averages with the Average Directional Index (ADX) as filters.

## Logic

- Long entry: Price above slow MA, fast MA above slow MA, price at least `MaDifference` points above fast MA for two last candles, ADX rising above `AdxMainLevel` with +DI rising and -DI falling.
- Short entry: Mirror conditions.
- Position is closed when profit in points reaches `CloseProfit` or when a limit order at `TakeProfit` distance is executed.

## Parameters

- `BigMaPeriod` (25) – period of the slower MA.
- `BigMaType` – type of the slower MA.
- `SmallMaPeriod` (5) – period of the faster MA.
- `SmallMaType` – type of the faster MA.
- `MaDifference` (5) – minimal distance between price and fast MA in points.
- `AdxPeriod` (11) – ADX calculation period.
- `AdxMainLevel` (13) – minimal ADX value.
- `AdxPlusLevel` (13) – minimal +DI value.
- `AdxMinusLevel` (14) – minimal -DI value.
- `TakeProfit` (299) – take-profit distance in points.
- `CloseProfit` (13) – profit in points for early exit.
- `Volume` (0.1) – trade volume.
- `CandleType` – candle timeframe (default H4).
