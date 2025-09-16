# Laguerre Filter

The strategy trades the crossover between a Laguerre filter and a short FIR filter built as a weighted moving average of recent median prices.

- The Laguerre filter smooths price using the Gamma parameter to reduce noise.
- The FIR line is a 4-period weighted moving average with symmetrical weights.
- When the FIR line was above the Laguerre line and crosses below it, the strategy opens a long position.
- When the FIR line was below and crosses above the Laguerre line, a short position is opened.
- Opposite positions are closed when the relation between the lines reverses.
- A stop-loss in percent of entry price protects every trade.

This mean-reversion approach attempts to capture pullbacks when price deviates from the smoothed Laguerre curve.
