# Fibonacci Potential Entries Retracement Strategy

## Overview
The **Fibonacci Potential Entries Retracement Strategy** recreates the MetaTrader expert `EA_PUB_FibonacciPotentialEntries`. The algorithm waits for live Level 1 quotes, then places two pending orders around manually supplied Fibonacci retracement levels. When the shared profit target is reached the strategy scales out of each position by 50% and moves the protective stop to break-even for the remaining quantity.

## Mapping of the original logic
- **Entry orders** – Two limit orders are emitted once both best bid and best ask prices are available:
  - *First order*: placed at the 50% retracement (`P50Level`). The stop-loss is anchored three spreads below (bull mode) or above (bear mode) the 61% level.
  - *Second order*: placed at the 61% retracement (`P61Level`) with the stop-loss defined three spreads away from the midpoint between the 61% and 100% levels.
- **Direction bias** – The original `bType` input becomes the `MarketBias` parameter (`Bull` for buy limits, `Bear` for sell limits).
- **Risk allocation** – The first trade always risks `0.7%` of portfolio equity. The second trade consumes the remaining portion of `RiskPercent` (`max(RiskPercent - 0.7, 0)`), keeping the split used by the EA.
- **Volume calculation** – Risk is translated to position size through `Portfolio.CurrentValue` (with fallbacks to `CurrentBalance` and `BeginValue`) together with the instrument's price step, step cost and multiplier.
- **Partial take-profit** – When price crosses `TargetLevel`, each filled trade sends a market order to close half of its open volume. Afterwards the stop order is moved to the recorded entry price, matching the EA's `OrderClose` + `OrderModify` sequence.

## Parameters
| Name | Description |
| --- | --- |
| `P50Level` | Price assigned to the 50% Fibonacci retracement. |
| `P61Level` | Price assigned to the 61.8% Fibonacci retracement. |
| `P100Level` | Price assigned to the 100% Fibonacci retracement (used for the midpoint stop). |
| `TargetLevel` | Shared profit target for both trades. |
| `RiskPercent` | Total risk budget in percent of equity (must be ≥ 0.7). |
| `MarketBias` | Chooses long (`Bull`) or short (`Bear`) campaign. |

## Execution details
1. Subscribe to Level 1 quotes via `SubscribeLevel1()` and wait for positive bid/ask values.
2. Compute spread, stop levels and position sizes. Orders are submitted once per run and won't be recreated automatically afterwards (same behaviour as the MQL expert).
3. Upon fills, the strategy records average entry price, places the appropriate stop order and tracks open volume per leg.
4. When the market prints beyond `TargetLevel`, the strategy sends one partial-close market order per leg and subsequently moves the stop to break-even for the remaining quantity.
5. Stop orders are cancelled when no volume remains or when the strategy stops.

## Notes and limitations
- The stop-loss is regenerated whenever the position size changes. If the broker rejects stop orders, check connector permissions and adjust exchange-specific settings accordingly.
- The take-profit is not registered as a pending order. Instead, the algorithm mirrors the EA by monitoring the price level and managing exits in real time.
- Because orders are created only once, restart the strategy to refresh pending orders after parameters change (identical to the MetaTrader workflow).
