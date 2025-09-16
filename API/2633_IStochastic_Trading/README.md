# IStochastic Trading Strategy

## Overview
IStochastic Trading Strategy is a direct StockSharp port of the "IStochastic_Trading" MetaTrader 5 expert advisor. The bot uses the Stochastic Oscillator to detect oversold and overbought conditions and then builds a martingale-style position ladder while managing every entry with stop loss, take profit and a trailing stop. The implementation operates on finished candles obtained through StockSharp's high-level API and relies on market orders only.

## Trading Logic
1. Calculate a Stochastic Oscillator with configurable %K length, %D smoothing and an additional slowing factor.
2. When there are no active positions, evaluate the most recent finished candle:
   - Open a long position if %K is above %D and %D is below the configured buy zone.
   - Open a short position if %K is below %D and %D is above the configured sell zone.
3. When a position exists, monitor the latest fill in the ladder:
   - If the market moves against the trade by at least the configured gap (in pips), open a new position in the same direction with twice the previous volume, as long as the maximum number of positions is not exceeded.
4. For every entry maintain per-trade stop loss and take profit levels derived from pip distances converted to price points using the security's `PriceStep` and number of decimals. If the closing price reaches the stop or the target, the strategy exits the specific position with a market order.
5. Apply a trailing stop after each candle close. When the trade moves far enough in the favourable direction, the stop price is tightened by the specified trailing step, approximating the terminal's per-position trailing behaviour.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `OrderVolume` | `0.1` | Initial position size in lots. Additional entries double the previous volume. |
| `TakeProfitPips` | `50` | Take profit distance measured in pips. The value is converted to price points internally. |
| `StopLossPips` | `50` | Stop loss distance in pips for each position. |
| `TrailingStopPips` | `10` | Trailing stop distance in pips. Set to zero to disable trailing. |
| `TrailingStepPips` | `5` | Minimum favourable move (in pips) before the trailing stop is adjusted. |
| `MaxPositions` | `3` | Maximum number of simultaneously open martingale steps. A value of `0` removes the limit. |
| `GapPips` | `7` | Price gap, in pips, required before doubling into the current direction. |
| `KPeriod` | `5` | Number of candles used to build the %K line. |
| `DPeriod` | `3` | Period of the %D smoothing average. |
| `Slowing` | `3` | Additional smoothing applied to %K. |
| `ZoneBuy` | `30` | %D threshold used to validate long entries (oversold zone). |
| `ZoneSell` | `70` | %D threshold used to validate short entries (overbought zone). |
| `CandleType` | `15-minute time frame` | Candle series employed for calculations. |

## Implementation Notes
- Pip distances are converted to prices with `PriceStep`. For 3- and 5-digit quotes an additional factor of 10 is used to mimic MetaTrader's adjusted point logic.
- Stop loss, take profit and trailing stop checks rely on closed candle prices to keep the logic deterministic inside the backtester. Real-time execution can be customised if intrabar management is required.
- The strategy only opens one directional ladder at a time; all positions must be closed before switching direction.
- Python implementation is intentionally omitted as requested.
