# NTK_07 Grid Strategy

## Overview

The NTK_07 strategy is a symmetric pending order grid originally written for MetaTrader 4. It places a pair of stop orders around the current price and manages a martingale-style position pyramid using configurable spacing, stop loss, take profit and trailing rules. The StockSharp port keeps the original behaviour while exposing every setting as a strongly typed strategy parameter.

The strategy continuously ensures that:

* A buy stop and a sell stop are parked around the market when there are no active orders.
* After a breakout is filled the opposite pending order is cancelled to prevent hedging.
* Additional orders in the same direction can be added at `Multiplier` times the previous size until the `LotLimit` would be exceeded.
* When no further scaling is allowed the active position is protected by a trailing stop and, optionally, a dynamically extended take profit.
* Protective stop and take-profit orders are recreated automatically whenever volumes or target prices change so that the entire open position always shares the same exit levels.

## Trading Logic

1. **Session filter.** Trading is skipped on Saturdays and Sundays or when the current hour is outside of `[StartHour, EndHour]`. The hour range matches the original MT4 logic: `EndHour = 24` allows trading during the whole day.
2. **Capital check.** When a portfolio is attached the current account value must be at least `MinCapital` before any order is created.
3. **Channel breakout (optional).** If `ChannelPeriod` is greater than zero, the highest high and lowest low of the last `ChannelPeriod` completed candles are tracked. Depending on `UseChannelCenter`:
   * `false` – both pending orders are submitted only if the ask price is outside of the detected range (breakout trading).
   * `true` – orders are submitted when price comes back to the midpoint of the range (mean-reversion style).
4. **Initial pending orders.** When there are no active orders a buy stop is placed `NetStepPips` above the best ask and a sell stop `NetStepPips` below the best bid. The base volume is defined by the money management module.
5. **Position scaling.** After an order is filled the opposite pending order is cancelled. If another order is already active in the same direction the next pending order is placed `NetStepPips` away using `RoundVolume(previousVolume × Multiplier)`. When the next volume would exceed the calculated `LotLimit` the strategy stops adding to the grid.
6. **Stop loss and take profit.** Every time the open position changes the strategy recreates a protective stop and (optionally) a take-profit order for the aggregated long or short exposure. The distances are derived from `StopLossPips` and `TakeProfitPips`.
7. **Break-even logic.** When `UseBreakEven = true` and price moves by `BreakEvenOffsetPips` beyond the last filled order, the stop loss is moved to the volume-weighted average entry price (rounded using `PriceRoundingFactor`).
8. **Trailing behaviour.** If the next scaling step is not allowed the strategy uses the highest/lowest candle price to move the stop towards the market by `TrailingStopPips`. When `TrailProfit = true` the take-profit distance is also shifted so it always remains `TakeProfitPips` away from the last candle extreme. When `UseMovingAverageFilter = true` and price is trading against the moving average, the trailing distance is cut in half, emulating the original half-step trailing behaviour around a moving average.

## Money Management

The port supports the three original money management rules through the `ManagementMode` parameter:

| Mode | Description |
| ---- | ----------- |
| `Fixed` | Use `InitialLot` for every new order and cap the per-order size at `LotLimit`. |
| `BalanceBased` | Recalculate the starting lot from the portfolio balance: `ceil(balance / 1000 × PercentRisk / 100)`. The result is repeatedly divided by `Multiplier` to project the smallest grid order, rounded by `LotRoundingFactor`. The original `LotLimit` becomes the theoretical maximum lot size. |
| `Progressive` | Keep `InitialLot` as the base volume but project the theoretical largest order by multiplying by `Multiplier` for each grid level. |

All orders are rounded using `LotRoundingFactor` (default 10 => 0.1 increments), while the break-even price is rounded with `PriceRoundingFactor` (default 10000 => 0.0001 increments).

## Parameters

| Name | Default | Description |
| ---- | ------- | ----------- |
| `NetStepPips` | 23 | Distance between consecutive grid levels. |
| `StopLossPips` | 115 | Stop-loss distance applied to every position. Set to 0 to disable. |
| `TakeProfitPips` | 300 | Take-profit distance for the aggregated position. Set to 0 to disable. |
| `TrailingStopPips` | 75 | Trailing stop distance activated once scaling is no longer possible. |
| `Multiplier` | 1.7 | Volume multiplier for the next grid level. |
| `TrailProfit` | `true` | When enabled the take-profit is shifted alongside the trailing stop. |
| `ManagementMode` | `Progressive` | Selected money management rule. |
| `InitialLot` | 1 | Base order volume. |
| `LotLimit` | 7 | Maximum lot size allowed for a single pending order. |
| `MaxTrades` | 4 | Maximum number of grid levels. |
| `PercentRisk` | 10 | Percentage of balance used in balance-based money management. |
| `MinCapital` | 5000 | Minimum portfolio value required before trading. |
| `UseBreakEven` | `false` | Enable break-even stop adjustments. |
| `BreakEvenOffsetPips` | 5 | Profit threshold (in pips) required for break-even. |
| `UseMovingAverageFilter` | `false` | Enables the moving-average-aware trailing logic. |
| `MovingAverageLength` | 100 | Length of the moving average used in the filter. |
| `MovingAverageShift` | 0 | Shift applied to the moving average (values from previous candles are used when > 0). |
| `StartHour` | 0 | Earliest allowed trading hour (0–23). |
| `EndHour` | 24 | Latest allowed trading hour (inclusive). |
| `ChannelPeriod` | 0 | Lookback window for the breakout/center filter. Set to 0 to disable the filter. |
| `UseChannelCenter` | `false` | Switch between breakout (`false`) and midpoint (`true`) style entries. |
| `LotRoundingFactor` | 10 | Divider used when rounding volumes. |
| `PriceRoundingFactor` | 10000 | Divider used when rounding the break-even price. |
| `CandleType` | 15-minute time frame | Working candle type for range detection and trailing calculations. |

## Implementation Notes

* Order books are subscribed to in order to obtain accurate best bid/ask values before placing pending orders. When the book is unavailable the strategy falls back to the candle close price.
* Protective stops and targets are re-created instead of modified, because the high-level API exposes safer helpers for registering fresh orders rather than mutating existing ones.
* Moving-average shift values beyond the available history fall back to the most recent value, preventing null references while keeping behaviour close to the MetaTrader implementation.
* All price calculations are normalised through `Security.ShrinkPrice` so that stop and limit levels always respect the instrument tick size.

## Usage Tips

1. Configure `Strategy.Volume` to define the notional trade size multiplier if your broker requires scaling relative to the portfolio size.
2. When testing symbols with exotic tick sizes adjust `LotRoundingFactor` and `PriceRoundingFactor` accordingly so that the rounding operations stay meaningful.
3. The default parameters were taken from the original EA for EURUSD H1 data between 2008-01-01 and 2008-11-01. Re-optimisation is recommended for other assets or timeframes.
4. Because the grid can accumulate a large directional exposure, always monitor the `LotLimit` and `MaxTrades` values to keep risk under control.
