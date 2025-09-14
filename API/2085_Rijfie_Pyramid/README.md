# Rijfie Pyramid Strategy

This strategy opens an initial long position when the Stochastic oscillator crosses above a configurable low level. It then adds new positions each time price drops by a fixed percentage while staying above an EMA filter and a minimum price. An optional timer can close all positions at a specified time.

## Parameters
- Candle Type
- Stochastic low level
- Maximum price for first entry
- Minimum allowed price
- EMA period
- Step level in percent
- Close positions at time
- Close hour
- Close minute
