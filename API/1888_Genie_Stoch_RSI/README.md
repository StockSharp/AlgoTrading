# Genie Stoch RSI Strategy

This strategy trades using a combination of the Relative Strength Index (RSI) and the Stochastic Oscillator.
It waits for the market to reach oversold or overbought zones and then looks for a cross between the Stochastic main
line and its signal line to confirm the reversal. A trailing stop and a fixed take profit are applied for risk
management.

## Logic

1. Subscribe to candles of the selected timeframe.
2. Calculate RSI with a configurable period.
3. Calculate Stochastic Oscillator with configurable %K, %D and slowing periods.
4. For a long entry:
   - RSI is below the oversold level.
   - %K is below the Stochastic oversold level.
   - Previous %K is below previous %D and current %K crosses above current %D.
5. For a short entry:
   - RSI is above the overbought level.
   - %K is above the Stochastic overbought level.
   - Previous %K is above previous %D and current %K crosses below current %D.
6. Position size is taken from the strategy `Volume` property. Existing positions are reversed when an opposite signal
   appears.
7. `StartProtection` enables a trailing stop and take profit measured in price points.

## Parameters

| Name | Description |
| ---- | ----------- |
| `RsiPeriod` | RSI calculation length. |
| `KPeriod` | Stochastic %K period. |
| `DPeriod` | Stochastic %D period. |
| `Slowing` | Stochastic slowing value. |
| `RsiOverbought` | RSI level considered overbought. |
| `RsiOversold` | RSI level considered oversold. |
| `StochOverbought` | Stochastic level considered overbought. |
| `StochOversold` | Stochastic level considered oversold. |
| `TakeProfit` | Take profit distance in price points. |
| `TrailingStop` | Trailing stop distance in price points. |
| `CandleType` | Candle type and timeframe to analyze. |

## Notes

The strategy processes only finished candles and ignores any signal until all indicators are fully formed.
It is intended as an educational example and should be tested thoroughly before live trading.
