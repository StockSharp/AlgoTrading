# Daily BreakPoint Strategy

## Overview
The **Daily BreakPoint Strategy** is a StockSharp port of the MetaTrader 5 expert advisor "Daily BreakPoint" (build 19498). The algorithm monitors the distance between the current price and the daily open. When the move from the daily open exceeds a configurable threshold and the previous candle meets strict body-size requirements, the strategy either enters in the direction of the breakout or reverses the existing exposure depending on the `CloseBySignal` flag.

The strategy works with two data streams at the same time:

1. Intraday candles defined by the `CandleType` parameter for signal generation.
2. Daily candles used to track the most recent session open price.

## Trading Logic
1. When a new intraday candle finishes, the strategy reads the latest daily open price and calculates the breakout levels using `BreakPointPips` (converted into absolute prices via the instrument tick size).
2. The body size of the recently closed candle must be within the range `[LastBarSizeMinPips, LastBarSizeMaxPips]`.
3. **Bullish setup**
   - The candle must close above its open (`Close > Open`).
   - The close must be at least `BreakPointPips` above the daily open.
   - The breakout price (daily open + breakpoint) must lie inside the candle body.
   - If `CloseBySignal = false`, the strategy opens a long position. Otherwise, it closes any open long exposure and establishes a short position.
4. **Bearish setup** mirrors the bullish case: a bearish candle whose close is at least `BreakPointPips` below the daily open and whose body contains the breakout level triggers either a short entry (`CloseBySignal = false`) or a reversal into a long position (`CloseBySignal = true`).
5. Orders are sent as market orders using the configured `OrderVolume`. Position size is cumulative, so multiple signals can scale the position in either direction.

## Risk Management
- **Stop Loss / Take Profit**: Optional fixed targets defined in pips (`StopLossPips`, `TakeProfitPips`). When set to zero the corresponding level is disabled. The strategy evaluates candle highs and lows to detect hits.
- **Trailing Stop**: Enabled when `TrailingStopPips > 0`. Once the open profit exceeds `TrailingStopPips + TrailingStepPips`, the stop is trailed behind the price by `TrailingStopPips`. The step parameter prevents frequent stop adjustments in flat markets.
- All price distances are converted from pips using the instrument `PriceStep`. For 3- or 5-decimal quoting the pip equals ten price steps, replicating the original expert advisor behaviour.

## Parameters
| Name | Description |
| --- | --- |
| `OrderVolume` | Base volume used for every market order. |
| `CloseBySignal` | If `true`, the strategy closes existing positions and opens the opposite direction when a breakout signal appears. |
| `BreakPointPips` | Distance from the daily open required to confirm a breakout. |
| `LastBarSizeMinPips` / `LastBarSizeMaxPips` | Minimum and maximum body size of the trigger candle. |
| `TrailingStopPips` | Trailing stop distance. Set to `0` to disable trailing. |
| `TrailingStepPips` | Additional move required before each trailing adjustment. |
| `StopLossPips` | Optional fixed stop loss. `0` disables it. |
| `TakeProfitPips` | Optional fixed take profit. `0` disables it. |
| `CandleType` | Intraday candle series used for signal generation. |

## Usage Notes
- The strategy automatically subscribes to both intraday and daily candles. Ensure that the data provider supports the requested time frames.
- Because the logic evaluates finished candles, orders are submitted at the close price of the signal bar.
- The pip conversion assumes Forex-style pricing. Review the defaults when applying the strategy to instruments with unconventional tick sizes.
