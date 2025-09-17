# Three Soldiers Stochastic Strategy

This strategy reproduces the MetaTrader expert `Expert_ABC_WS_Stoch.mq5`, which combines classical three-candle reversal patterns with Stochastic oscillator confirmation. A long signal requires the bullish "Three White Soldiers" formation together with an oversold Stochastic signal line, while a short signal relies on the bearish "Three Black Crows" confirmed by an overbought Stochastic. The exit logic monitors crossovers of the signal line through configurable bands to close positions.

## Trading Logic

1. **Pattern detection**
   - Track the latest three completed candles.
   - Identify *Three White Soldiers* when all three candles are bullish and each close is higher than the previous one.
   - Identify *Three Black Crows* when all three candles are bearish and each close is lower than the previous one.
2. **Oscillator confirmation**
   - Calculate a Stochastic oscillator with `%K`, `%D`, and `Slowing` periods identical to the original expert (47, 9, 13 by default).
   - Use the signal line (`%D`) as confirmation:
     - Enter long if the previous signal line value is below the oversold threshold (default `30`).
     - Enter short if the previous signal line value is above the overbought threshold (default `70`).
3. **Exit conditions**
   - Close a long trade when the signal line crosses above either the lower or upper exit thresholds (default `20` and `80`).
   - Close a short trade when the signal line crosses back below these thresholds.
   - Both exit checks rely on the previous and pre-previous signal line values to detect genuine crossovers.

## Parameters

| Name | Default | Description |
|------|---------|-------------|
| `CandleType` | `1h` time frame | Time frame for the candle subscription. |
| `StochKPeriod` | `47` | Lookback period for `%K`. |
| `StochDPeriod` | `9` | Moving average length for the signal line. |
| `StochSlowing` | `13` | Additional smoothing applied to `%K`. |
| `OversoldLevel` | `30` | Signal line level required to confirm a long entry. |
| `OverboughtLevel` | `70` | Signal line level required to confirm a short entry. |
| `ExitLowerLevel` | `20` | Lower bound used for long exit crossovers. |
| `ExitUpperLevel` | `80` | Upper bound used for short exit crossovers. |

All numeric parameters support optimization ranges matching the MetaTrader template, so the behavior can be fine-tuned through the Strategy Designer.

## Order Management

- The strategy reverses positions when an opposite signal appears by adding the absolute size of the current position to the configured `Volume`.
- `StartProtection()` is enabled to integrate with the platform risk controls, although no explicit stop-loss or take-profit levels are enforced by default.

## Visualization

When executed inside the Strategy Designer, the strategy draws:

- Price candles for the selected symbol and time frame.
- The configured Stochastic oscillator.
- Trade markers to highlight entries and exits.

## Usage Notes

- Confirm that the instrument provides enough history for the Stochastic oscillator to warm up before expecting signals.
- Consider pairing the strategy with additional risk filters (volatility, session filters, etc.) when deploying live.
- The thresholds are exposed as parameters, enabling rapid experimentation with different confirmation bands without editing code.
