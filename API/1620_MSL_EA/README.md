# MSL EA

Breakout strategy that finds local highs and lows and shifts them by a fixed number of ticks.
A long position is opened when the price closes above the upper level, and a short position is opened when it closes below the lower level.

## Parameters
- **Level** — number of consecutive fractal levels.
- **Distance** — offset in ticks from the level.
- **Max Trades** — maximum simultaneous trades.
- **Candle Type** — candle series used for analysis.
