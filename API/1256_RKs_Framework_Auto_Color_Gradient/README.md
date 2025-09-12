# RK's Framework Auto Color Gradient Strategy

Combines Bollinger Bands %B and RSI into a single oscillator, maps it to a color gradient and trades when it crosses the center line.

## Logic
- Calculates Bollinger Bands %B and Relative Strength Index.
- Normalizes both with a stochastic process and averages them.
- Converts the result to a selectable color gradient.
- Buys when the averaged value is above zero.
- Sells when the averaged value is below zero.
