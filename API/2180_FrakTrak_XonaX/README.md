# FrakTrak XonaX Strategy

FrakTrak XonaX is a breakout strategy based on fractal levels calculated on a higher timeframe. When price moves beyond the most recent fractal by a small offset the strategy enters in the direction of the breakout. A fixed take profit and trailing stop manage the open position.

## Parameters
- **Volume** – order size.
- **Take Profit** – distance in points for the take-profit level.
- **Trailing Stop** – distance in points used for trailing the stop-loss.
- **Trailing Correction** – additional distance added to the trailing stop.
- **Candle Type** – timeframe used to build candles and fractals.

## Trading rules
1. Calculate upper and lower fractals using the last completed candles.
2. Buy when the close price exceeds the upper fractal plus 15 points and no long position exists. Stop-loss is placed at the last lower fractal and take-profit is set using *Take Profit*.
3. Sell when the close price falls below the lower fractal minus 15 points and no short position exists. Stop-loss is placed at the last upper fractal and take-profit is set using *Take Profit*.
4. After a position becomes profitable more than *Trailing Stop* points, the stop-loss trails behind price with an additional *Trailing Correction* offset.
