# Flat Channel Breakout Strategy

The **Flat Channel Strategy** is a C# translation of the MetaTrader 5 expert advisor *Flat Channel (barabashkakvn's edition)*. It keeps the original workflow: a smoothed standard deviation highlights volatility squeezes, the highest and lowest prices inside the squeeze define a horizontal channel, and pending stop orders are placed just outside of that range. When the market breaks out the strategy joins the move with predefined stop-loss and take-profit levels and can optionally trail the stop as the position gains profit.

## How it works

1. **Volatility squeeze detection** – A `StandardDeviation` indicator with length `StdDevPeriod` is smoothed by a short `SimpleMovingAverage` of `SmoothingLength`. Whenever the smoothed series prints `FlatBars` consecutive non-increasing values the market is treated as flat and the order flags are re-armed.
2. **Channel construction** – Once a flat is confirmed, the strategy requests the highest high and lowest low over the last `max(ChannelLookback, FlatBars + 1)` candles using the built-in `Highest`/`Lowest` indicators. The channel height is filtered by `ChannelMinPips`/`ChannelMaxPips` after converting pips into price units through `PipSize` (or the detected tick size when the parameter is left at zero).
3. **Pending orders** – If the current position is flat and trading is allowed, the strategy submits a buy stop at `high + IndentPips` and a sell stop at `low − IndentPips`. Each order remembers the protective levels that were calculated at submission time.
4. **Breakout execution** – When a pending order fills, the opposite pending order is cancelled automatically. The filled price becomes the entry anchor for trailing-stop logic and the memorised stop-loss / take-profit distances are activated.
5. **Position management** – The active position is supervised on every completed candle. If price touches the stop-loss or take-profit level the strategy issues a market exit. When `TrailingStopPips` is greater than zero the stop is pulled forward once the close price moves at least `TrailingStopPips + TrailingStepPips` away from the fill price.
6. **Session filter** – When `UseTradingHours` is enabled the breakout logic only runs between `StartHour` (inclusive) and `EndHour` (exclusive). Overnight sessions are supported by allowing `StartHour > EndHour`.

## Risk management

- **Dynamic or fixed protection** – Set `StopLossPips` / `TakeProfitPips` to positive values to use fixed distances (in pips). Keeping them at zero switches to dynamic sizing based on the channel height and the `DynamicStopMultiplier` / `DynamicTakeMultiplier` coefficients.
- **Trailing stop** – Enable `TrailingStopPips` to follow the move once the trade is in profit. The trailing logic respects `TrailingStepPips` to avoid micro adjustments.
- **Position cap** – `MaxPositions` limits the aggregated exposure to `MaxPositions × TradeVolume`. If that threshold is reached no new pending orders are submitted until the exposure decreases.
- **Directional filters** – `UseBuy` and `UseSell` allow the strategy to operate in breakout-only, breakdown-only or bi-directional modes.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `TradeVolume` | `1` | Volume submitted with every pending order. |
| `PipSize` | `0.0001` | Manual pip size override. Leave at zero to use the security tick size (with automatic 3/5-digit adjustment). |
| `StdDevPeriod` | `46` | Lookback for the base `StandardDeviation`. |
| `SmoothingLength` | `3` | Moving average length applied to the volatility series. |
| `FlatBars` | `3` | Number of consecutive non-increasing smoothed volatility values required to re-arm breakout orders. |
| `ChannelLookback` | `5` | Candles used to measure the highest high and lowest low once a flat is detected. Automatically compared with `FlatBars + 1`. |
| `ChannelMinPips` | `15` | Minimum channel height (in pips). Set to `0` to disable the lower bound. |
| `ChannelMaxPips` | `105` | Maximum channel height (in pips). Set to `0` to disable the upper bound. |
| `DynamicStopMultiplier` | `1` | Channel-height multiplier used for dynamic stop-loss calculation when `StopLossPips = 0`. |
| `DynamicTakeMultiplier` | `1` | Channel-height multiplier used for dynamic take-profit calculation when `TakeProfitPips = 0`. |
| `StopLossPips` | `0` | Fixed stop-loss distance in pips. Overrides the dynamic formula when positive. |
| `TakeProfitPips` | `0` | Fixed take-profit distance in pips. Overrides the dynamic formula when positive. |
| `IndentPips` | `0` | Additional offset (in pips) added beyond the channel boundaries for pending orders. |
| `TrailingStopPips` | `5` | Trailing stop distance in pips. Set to `0` to disable trailing. |
| `TrailingStepPips` | `5` | Minimum step (in pips) required to move the trailing stop. |
| `UseBuy` | `true` | Enable long (buy stop) breakouts. |
| `UseSell` | `true` | Enable short (sell stop) breakouts. |
| `MaxPositions` | `5` | Maximum number of base volumes allowed in the aggregated position. |
| `UseTradingHours` | `true` | Enable the trading session filter. |
| `StartHour` | `0` | Session start hour (inclusive). |
| `EndHour` | `23` | Session end hour (exclusive). |
| `CandleType` | `H1` | Candle series used for calculations (defaults to 1-hour time frame). |

## Notes

- The strategy operates exclusively on completed candles via the high-level `SubscribeCandles().Bind(...)` API, matching the deterministic behaviour expected from the original EA.
- Protective prices are normalised through `Security.ShrinkPrice` to respect exchange tick sizes.
- When both pending orders are active and one of them fills, the opposite order is cancelled immediately so that only one breakout position can be open at a time.
