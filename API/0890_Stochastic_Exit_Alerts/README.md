# Stochastic Exit Alerts Strategy

This strategy enters long when the Stochastic %K line crosses above %D in the oversold area and enters short when %K crosses below %D in the overbought area. Positions are protected by fixed stop loss and take profit measured in ticks. When an opposite crossover occurs outside the extreme zone, the position is closed without reversing.

## Parameters
- `StochLength` – main period of the Stochastic oscillator.
- `KLength` – smoothing period for the %K line.
- `DLength` – smoothing period for the %D line.
- `StopLossTicks` – stop loss distance in ticks.
- `TakeProfitTicks` – take profit distance in ticks.
- `CandleType` – candle time frame.
