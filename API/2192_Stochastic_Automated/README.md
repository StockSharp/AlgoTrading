# Stochastic Automated Strategy

This strategy trades using the **Stochastic Oscillator** on the selected candle timeframe. It waits for %K and %D to enter extreme zones and then acts on crossovers to open positions. Fixed take profit and stop loss protect each trade, while a trailing stop locks in profits.

## Logic

1. **Entry**
   - **Long:**
     - Both %K and %D are below `OverSold` two candles ago.
     - %D was above %K two candles ago and below %K one candle ago.
     - %D is rising.
   - **Short:**
     - Both %K and %D are above `OverBought` two candles ago.
     - %D was below %K two candles ago and above %K one candle ago.
     - %D is falling.
2. **Exit**
   - Position is closed when Stochastic leaves the extreme zone or %D turns in the opposite direction.
   - A trailing stop exits if price retraces by `TrailingStop`.
   - Global `TakeProfit` and `StopLoss` are applied to every trade.

## Parameters

| Name | Description |
|------|-------------|
| `CandleType` | Time frame for Stochastic calculations. |
| `KPeriod` | Lookback period for %K line. |
| `DPeriod` | Smoothing period for %D line. |
| `Slowing` | Additional smoothing for %K. |
| `OverBought` | Upper threshold indicating overbought market. |
| `OverSold` | Lower threshold indicating oversold market. |
| `TakeProfit` | Distance from entry for profit target (price units). |
| `StopLoss` | Distance from entry for protective stop (price units). |
| `TrailingStop` | Trailing distance once the trade moves in profit (price units). |

## Indicators

- `StochasticOscillator`

## Notes

- Comments in code are in English.
- Python version is intentionally omitted.
