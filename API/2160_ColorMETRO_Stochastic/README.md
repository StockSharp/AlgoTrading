# ColorMETRO Stochastic Strategy

This strategy is a C# port of the MQL5 expert **exp_colormetro_stochastic.mq5**. It replaces the original ColorMETRO Stochastic indicator with the built-in `StochasticOscillator` from StockSharp and trades on crossover events.

## Logic
- Subscribes to 8-hour candles by default (configurable).
- Calculates the Stochastic oscillator with parameters:
  - %K period (`KPeriod`)
  - %D period (`DPeriod`)
  - Additional smoothing (`Slowing`)
- Stores previous %K and %D values to detect crossovers.
- **Buy** when %K crosses above %D.
- **Sell** when %K crosses below %D.
- Applies a simple 2% stop-loss and take-profit via `StartProtection`.

## Parameters
| Name | Description |
|------|-------------|
| `KPeriod` | Lookback for %K line (default 5). |
| `DPeriod` | Smoothing period for %D line (default 3). |
| `Slowing` | Additional smoothing value (default 3). |
| `CandleType` | Timeframe of candles, default 8 hours. |

## Notes
The original MQL version used a custom ColorMETRO Stochastic indicator with fast and slow step lines. This port approximates its signals using the standard Stochastic oscillator.
