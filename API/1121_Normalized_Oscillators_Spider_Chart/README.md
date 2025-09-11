# Normalized Oscillators Spider Chart Strategy

This strategy calculates multiple oscillators (RSI, Stochastic, Correlation, Money Flow Index, Williams %R, Percent Up, Chande Momentum Oscillator, and Aroon Oscillator). All values are normalized into the 0-1 range and averaged to generate trading signals. The strategy buys when the average exceeds 0.6 and sells short when it drops below 0.4.

## Inputs
- **Length** — lookback period for all oscillators
- **Candle type** — time frame of candles used

## Notes
This is a simplified port of the TradingView script "Normalized Oscillators Spider Chart [LuxAlgo]" demonstrating indicator usage in StockSharp.
