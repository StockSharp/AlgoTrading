# Exp Oracle Strategy

This strategy is a C# port of the MetaTrader **Exp_Oracle** expert advisor. It relies on a custom *Oracle* indicator that blends the Relative Strength Index (RSI) and the Commodity Channel Index (CCI) to forecast market direction several bars ahead. The indicator generates two lines:

- **Oracle line** – raw combination of CCI and RSI extremes.
- **Signal line** – smoothed moving average of the Oracle line.

The strategy provides three trading modes to interpret these lines:

1. **Breakdown** – opens positions when the signal line crosses the zero level.
2. **Twist** – reacts to local turning points of the signal line.
3. **Disposition** – trades on crossings between the signal and the Oracle line.

## Parameters

- `OraclePeriod` – period for RSI and CCI calculations.
- `Smooth` – number of bars used to smooth the signal line.
- `Mode` – algorithm used to generate trading signals (`Breakdown`, `Twist`, or `Disposition`).
- `CandleType` – timeframe of incoming candles.
- `AllowBuy` – enables long entries.
- `AllowSell` – enables short entries.
- `Volume` – strategy volume inherited from the base `Strategy` class.

## Entry and Exit Rules

### Breakdown
- **Buy** when the signal line crosses above zero.
- **Sell** when the signal line crosses below zero.

### Twist
- **Buy** when the signal line turns upward after a decline.
- **Sell** when the signal line turns downward after a rise.

### Disposition
- **Buy** when the signal line crosses above the Oracle line.
- **Sell** when the signal line crosses below the Oracle line.

Existing positions are closed and reversed when an opposite signal appears. The strategy uses market orders for simplicity.

## Indicator Logic

For each bar:
1. Calculate RSI and CCI with the specified `OraclePeriod`.
2. Build four divergence values using differences between recent CCI and RSI values.
3. The Oracle line is the sum of the maximum and minimum divergence.
4. The Signal line is the simple moving average of the Oracle line over `Smooth` bars.

This approach attempts to predict short-term price movement by combining momentum (RSI) and channel (CCI) information.

## Notes

- The strategy operates on completed candles only.
- Protective stops are not implemented; use external risk controls if necessary.

