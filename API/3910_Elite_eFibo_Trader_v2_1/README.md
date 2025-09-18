# Elite eFibo Trader v2.1 Strategy

## Overview
Elite eFibo Trader v2.1 recreates the MetaTrader expert advisor that stacks Fibonacci-sized orders in one direction while sharing a common protective stop. The StockSharp port keeps the original behaviour: a single market order launches a sequence of stop orders spaced by `LevelDistancePips`, and every filled tier increases the exposure according to the Fibonacci progression. The strategy immediately closes the entire basket once the shared stop is touched or when the floating profit reaches `MoneyTakeProfit`.

The algorithm is intentionally directional. Set `OpenBuy` to `true` (and `OpenSell` to `false`) to trade bullish pullbacks, or flip the switches to run the bearish variant. Only one ladder is active at a time, mirroring the single-cycle logic from the MQL4 script.

## Data requirements
- Subscribes to the trade stream to retrieve the latest execution price used for ladder placement, trailing logic, and money take-profit evaluation.
- Relies on the security metadata (`PriceStep`, `StepPrice`, `VolumeStep`) to translate MetaTrader-style pip inputs into exchange prices and lot sizes.

## Ladder construction
1. When there is no exposure and trading is allowed, the strategy checks the direction switches. Exactly one of `OpenBuy` or `OpenSell` must be true; otherwise no ladder is started.
2. The first Fibonacci level is opened at market. Subsequent levels are scheduled as stop orders offset by `LevelDistancePips * pipSize` from the reference price recorded when the ladder starts.
3. Volumes come from the `Level1Volume` … `Level14Volume` parameters and are normalised to the security `VolumeStep`.
4. All levels inherit the same stop offset: `StopLossPips * pipSize`. The stop price is computed per fill and later tightened so that every active order shares the closest protective level.

## Stop management
- Each filled order stores its entry price and initial stop derived from the pip offset.
- On every trade tick the strategy re-evaluates all open stops and aligns them to the tightest value across the ladder (highest stop for longs, lowest stop for shorts) to mimic the repeated `OrderModify` calls from MetaTrader.
- When the last trade price crosses any shared stop the strategy cancels remaining pending orders and closes the whole basket with market orders.

## Money management
- Unrealised profit is calculated from the instrument `PriceStep` and `StepPrice` so that the cash target mirrors the `OrderProfit()` readings from MetaTrader.
- If the floating profit reaches or exceeds `MoneyTakeProfit`, all positions are closed and pending orders are cancelled immediately.
- When `TradeAgainAfterProfit` is `false`, the strategy remains idle after hitting the money target until it is restarted manually.

## Parameters
| Name | Description |
| ---- | ----------- |
| `OpenBuy` | Allow the strategy to build a bullish ladder (must be exclusive with `OpenSell`). |
| `OpenSell` | Allow the strategy to build a bearish ladder (must be exclusive with `OpenBuy`). |
| `TradeAgainAfterProfit` | Resume trading after the basket closes on the money take-profit. |
| `LevelDistancePips` | Distance in MetaTrader pips between consecutive stop orders. |
| `StopLossPips` | Distance in MetaTrader pips used to derive the protective stop for every filled level. |
| `MoneyTakeProfit` | Cash profit target that closes the entire basket. |
| `Level1Volume` … `Level14Volume` | Volumes used for each Fibonacci level; set to zero to skip a tier. |

## Implementation notes
- The pip conversion follows the MetaTrader convention: if the symbol has 3 or 5 decimals the effective pip equals `PriceStep * 10`.
- `StartProtection()` is called once during start-up to enable the built-in StockSharp safety checks.
- The shared stop logic intentionally keeps all orders in sync; once a tighter stop appears it is propagated to every active level.
- Pending orders are cleaned automatically whenever the ladder is flat, replicating the multiple `subCloseAllPending()` calls found in the MQL code.

## Usage tips
- Ensure that `PriceStep`, `StepPrice`, and `VolumeStep` are configured on the instrument; otherwise pip conversions or money targets may be inaccurate.
- Averaging systems can accumulate large exposure quickly. Verify the volume limits and margin requirements before running the strategy live.
- Disable `TradeAgainAfterProfit` to reproduce the one-shot behaviour where the EA stops trading after closing a profitable basket.
