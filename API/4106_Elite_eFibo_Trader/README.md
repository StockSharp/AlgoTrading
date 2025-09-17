# Elite eFibo Trader Strategy

## Overview
Elite eFibo Trader reproduces the averaging expert advisor that opens a Fibonacci progression of orders while monitoring a moving-average crossover and an optional RSI filter. The StockSharp port keeps the original basket logic: a market entry triggers a stack of pending stop orders spaced by configurable pip distances, and every additional fill increases exposure following the Fibonacci sequence. The strategy automatically flattens the basket once the floating profit reaches a cash target or when the trend filter turns against the current exposure.

## Market data
- Subscribes to a single configurable candle type (default: 15-minute candles).
- Uses the candle close for indicator values and to evaluate trailing/stop conditions.

## Entry logic
1. Direction is determined either by the moving-average crossover (enabled by default) or by the manual `ManualOpenBuy`/`ManualOpenSell` toggles.
2. When the MA logic is active, a bullish crossover (`fast` above `slow`) arms buy baskets and a bearish crossover arms sell baskets. A single signal per candle is enforced.
3. If the RSI filter is enabled, long baskets require `RSI > RsiHigh` while short baskets require `RSI < RsiLow`.
4. A new ladder is opened only when there are no active orders or positions from the strategy and trading is allowed (`TradeAgainAfterProfit`).
5. The first level is opened with a market order, while the remaining levels are submitted as stop orders offset by `LevelDistancePips`. Volumes follow the Fibonacci sequence and can be adjusted level by level.

## Exit logic
- Each filled level receives an initial stop calculated from `StopLossPips` and participates in a trailing update when the MA logic detects an adverse crossover.
- Stops are trailed to `close - TrailingStopPips` for long baskets and `close + TrailingStopPips` for short baskets, never moving further away than the current stop.
- When the price touches a level stop (based on candle high/low), the strategy closes the remaining volume of that level with a market order.
- If the floating profit of the basket (calculated from instrument `PriceStep` and `StepPrice`) reaches `MoneyTakeProfit`, all positions are closed and pending orders are cancelled.
- After the basket is flat, any pending stop orders are cancelled automatically. If `TradeAgainAfterProfit` is `false` the strategy remains idle until it is reset.

## Parameters
| Name | Description |
| ---- | ----------- |
| `UseMaLogic` | Enable or disable the moving-average crossover logic that sets the trade direction. |
| `MaSlowPeriod`, `MaFastPeriod` | Periods of the slow and fast SMAs. |
| `TrailingStopPips` | Pip distance used by the protective trailing stop when the trend filter turns adverse. |
| `UseRsiFilter`, `RsiPeriod`, `RsiHigh`, `RsiLow` | RSI filter configuration. The filter allows longs above `RsiHigh` and shorts below `RsiLow`. |
| `ManualOpenBuy`, `ManualOpenSell` | Manual toggles used when MA logic is disabled. |
| `TradeAgainAfterProfit` | Resume trading after reaching the money take-profit. |
| `LevelDistancePips` | Distance in pips between consecutive pending orders. |
| `StopLossPips` | Initial stop offset for every level. |
| `MoneyTakeProfit` | Cash profit target evaluated on the basket’s open PnL. |
| `Level1Volume` … `Level14Volume` | Volume of each Fibonacci level. Set to zero to disable a level. |
| `CandleType` | Timeframe/data type used for indicators. |

## Implementation notes
- Pip distances are converted from MetaTrader-style points by multiplying the instrument `PriceStep` by ten when the security has 3 or 5 decimal places. This mirrors the original `MyPoint` adjustment for 5-digit FX quotes.
- Each level is tracked independently. The strategy stores entry price, remaining volume, and stop level so partial fills and individual stop-outs are handled in the same way as the MQL expert.
- Floating profit is computed from `PriceStep` and `StepPrice`. Ensure those instrument properties are configured, otherwise the money take-profit will not trigger correctly.
- `StartProtection()` is invoked once during startup to enable the built-in safety checks from the StockSharp strategy base class.
- When no open volume remains, `CancelAllPendingOrders()` is called automatically, replicating the repeated `subCloseAllPending()` calls from the original script.

## Usage tips
- Verify the broker settings for `PriceStep`, `StepPrice`, `VolumeStep`, and minimum lot size to ensure Fibonacci volumes translate to valid orders.
- The strategy relies on candle data; make sure the selected timeframe matches the intended MetaTrader chart period.
- Consider running the strategy on demo feeds first: averaging systems can accumulate large exposure during adverse trends.
- Disable `UseMaLogic` to reproduce the manual bias used in the original EA inputs, or keep it enabled for automatic trend detection.
