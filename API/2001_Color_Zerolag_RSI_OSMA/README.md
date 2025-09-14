# Color Zerolag RSI OSMA Strategy

This strategy uses a composite oscillator built from five RSI calculations with different periods. The weighted sum of the RSI values is smoothed twice to produce a zero-lag OSMA line.

## How It Works

1. Calculate five RSI values with periods 8, 21, 34, 55, and 89.
2. Multiply each RSI by its weight and sum the results.
3. Apply two smoothing steps to the sum to obtain the OSMA value.
4. If the OSMA turns upward (previous value was lower than two bars ago and current value exceeds previous), the strategy closes short positions and optionally opens a long.
5. If the OSMA turns downward (previous value was higher than two bars ago and current value falls below previous), the strategy closes long positions and optionally opens a short.

## Parameters

- **Smoothing 1, Smoothing 2** – lengths of the smoothing phases.
- **Factor 1..5** – weights for each RSI component.
- **RSI Period 1..5** – periods of the RSI indicators.
- **Allow Buy / Allow Sell** – enable opening long or short positions.
- **Close Long / Close Short** – close existing positions on opposite signals.
- **Candle Type** – timeframe of the processed candles (default 4 hours).

## Notes

The strategy operates only on finished candles. Position protection is started automatically when the strategy begins.
