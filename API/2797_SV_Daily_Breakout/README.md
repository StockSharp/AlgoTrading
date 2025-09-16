# SV Daily Breakout Strategy

## Overview
The **SV Daily Breakout Strategy** is a direct C# conversion of the “SV v.4.2.5” MetaTrader 5 expert advisor. The system evaluates price action once per completed bar and allows at most one trade per exchange day. Trading begins only after the configured start time and relies on the relationship between the recent high/low range and two smoothed moving averages. A long position is opened when the full analysed range stays below both averages, signalling an anticipated rebound from oversold conditions. Conversely, a short position is opened when the range remains above both averages, signalling a potential reversal from overbought territory.

## Trading Rules
### Entry conditions
- **Daily gate** – no trades are evaluated until the current server time is later than *Start Hour*/*Start Minute*. Only one entry is permitted per day.
- **Data window** – the strategy skips the most recent `Shift` bars and analyses the next `Interval` bars. Their highest and lowest prices are compared against the shifted moving averages.
- **Long entry** – if the highest price in the analysed window is strictly below the slow MA **and** the lowest price is strictly below the fast MA, enter long (closing any existing short position first).
- **Short entry** – if the lowest price in the analysed window is strictly above the slow MA **and** the highest price is strictly above the fast MA, enter short (closing any existing long position first).

### Exit management
- **Initial stop loss** – placed `Stop Loss (pips)` away from the entry price. If the level is hit, the position is closed.
- **Take profit** – placed `Take Profit (pips)` away from the entry price. If the level is hit, the position is closed.
- **Trailing stop** – when enabled (both trailing distance and step are greater than zero), the stop moves in the direction of profit. For longs the stop is raised to `Close − Trailing Stop` once price advances more than `Trailing Stop + Trailing Step`; shorts mirror the logic.
- **Daily lockout** – regardless of how a trade exits, the strategy will not open a new position until the next trading day.

### Position sizing
- **Manual mode** – when *Use Manual Volume* is `true`, the strategy sends the fixed *Volume* value (adjusted to the instrument volume step).
- **Risk-based mode** – when *Use Manual Volume* is `false`, the strategy estimates the trade size from account equity and `Risk %`. It divides the risk capital by the monetary value of the configured stop distance, using instrument step information when available.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| Use Manual Volume | `false` | Use the fixed `Volume` value instead of risk-based sizing. |
| Volume | `0.1` | Trade volume when manual sizing is enabled. |
| Risk % | `5` | Percentage of account equity risked per trade when manual sizing is active. |
| Stop Loss (pips) | `50` | Stop-loss distance in pips. Set to `0` to disable. |
| Take Profit (pips) | `50` | Take-profit distance in pips. Set to `0` to disable. |
| Trailing Stop (pips) | `5` | Trailing stop distance in pips. Requires `Trailing Step` to be greater than zero. |
| Trailing Step (pips) | `5` | Minimal profit increment before the trailing stop is moved. |
| Start Hour | `19` | Hour (exchange time) when entries may start. |
| Start Minute | `0` | Minute (exchange time) when entries may start. |
| Shift | `6` | Number of newest bars excluded before analysing the range. |
| Interval | `27` | Number of historical bars used to compute the high/low window. |
| Fast MA Period | `14` | Length of the fast moving average. |
| Fast MA Shift | `0` | Horizontal shift (bars ago) used for the fast MA value. |
| Fast MA Method | `Smma` | Moving average method for the fast MA. |
| Fast Applied Price | `Median` | Price source for the fast MA. |
| Slow MA Period | `41` | Length of the slow moving average. |
| Slow MA Shift | `0` | Horizontal shift (bars ago) used for the slow MA value. |
| Slow MA Method | `Smma` | Moving average method for the slow MA. |
| Slow Applied Price | `Median` | Price source for the slow MA. |
| Candle Type | `1 hour` | Candle series used for calculations. |

## Additional Notes
- The conversion keeps the original behaviour of analysing a delayed price window (`Shift` + `Interval`) to avoid the most recent bars when determining breakouts.
- Trailing logic uses the candle close price to approximate MetaTrader’s tick-based trailing updates. Adjust the pip distances if your instrument requires different precision.
- Risk-based sizing relies on `Security.PriceStep`, `Security.StepPrice`, and `Security.VolumeStep`. Provide these values in your instrument settings for accurate lot sizing.
- The strategy calls `StartProtection()` so you can attach additional global risk rules if needed.
- To mirror the original EA, make sure your data feed and trading account operate on the same server time zone referenced by the *Start Hour* and *Start Minute* parameters.
