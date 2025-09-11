# Forex Pair Yield Momentum Strategy

This strategy trades a selected forex pair using the momentum of the 2-year yield spread between its currencies. The momentum is measured as the difference between the spread and its moving average. Bollinger Bands applied to the momentum define overbought and oversold zones. Positions are closed after a fixed number of bars.

## Key Features

- Uses 2-year yield spread momentum for signals.
- Bollinger Bands on momentum identify extreme conditions.
- Optional reversal of entry logic.
- Closes positions after a specified number of bars.

## Parameters

| Name | Description |
|------|-------------|
| `YieldASecurity` | First yield security. |
| `YieldBSecurity` | Second yield security. |
| `CandleType` | Candle timeframe for analysis. |
| `MomentumLength` | Period for yield spread average. |
| `BollingerLength` | Period for Bollinger Bands. |
| `BollingerStdDev` | Standard deviation multiplier for bands. |
| `HoldPeriods` | Bars to hold a position. |
| `ReverseLogic` | Invert long and short conditions. |

## Complexity

Beginner

