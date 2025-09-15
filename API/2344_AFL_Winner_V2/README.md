# AFL Winner V2 Strategy

## Overview

This sample strategy replicates the logic of the AFL Winner V2 indicator using the high-level StockSharp API. The indicator is approximated by a stochastic oscillator and signals are derived from its relative position and predefined threshold levels.

## Strategy Logic

- Use a `StochasticOscillator` to emulate the AFL Winner behavior.
- Open a long position when the oscillator indicates strong upward momentum.
- Open a short position when the oscillator signals strong downward momentum.
- Close longs when the color state falls below the neutral zone.
- Close shorts when the color state rises above the neutral zone.
- Parameters allow optimization of oscillator periods and threshold levels.

## Parameters

| Parameter   | Description                       |
|-------------|-----------------------------------|
| `KPeriod`   | %K period of the stochastic oscillator. |
| `DPeriod`   | %D period of the stochastic oscillator. |
| `HighLevel` | Upper threshold for bullish signals. |
| `LowLevel`  | Lower threshold for bearish signals. |

## Files

- `CS/AflWinnerV2Strategy.cs` – core strategy implementation.

## Notes

The strategy operates on finished candles only and uses automatic position protection to avoid unintended exposure.
