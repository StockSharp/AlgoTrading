# Trailing Take Profit Strategy

## Overview
This strategy reproduces the MetaTrader trailing take profit expert advisor. It does not search for trade entries. Instead it supervises the current position, keeps a limit order at the take profit level and gradually moves that order closer to the market when price starts pulling back. The logic is intended to secure accumulated profits while leaving enough distance for the trend to continue.

## How it works
1. When a new position appears (either long or short) the strategy immediately registers a take profit limit order at the entry price ± `TakeProfitPoints` converted into price units.
2. Bid/ask updates are received through a Level1 subscription. Each update recalculates the preferred take profit target.
3. If the recalculated target is at least `TrailingStepPoints` (price steps) closer to the current market than the previous order, the previous limit order is cancelled and a new one is placed at the tighter level.
4. A safety filter prevents the take profit from dropping below the break-even level defined by `BreakevenPoints`, unless `TrailInLossZone` is enabled.
5. Every target is also checked against a stop level distance equal to `SpreadMultiplier × price step` to avoid violating broker stop restrictions.
6. When the position is flat the pending take profit order is cancelled.

The strategy only controls orders for the instrument assigned to `Strategy.Security`. Any manual trades or trades from other strategies will be trailed as soon as the position value changes.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `TakeProfitPoints` | Distance in price steps used to place the initial take profit. | `100` |
| `TrailingStepPoints` | Minimum reduction (in price steps) that must be achieved before the take profit order is moved. | `10` |
| `TrailInLossZone` | When enabled, allows the take profit to trail even inside the loss area; otherwise the target is clamped to the break-even level. | `false` |
| `BreakevenPoints` | Profit buffer preserved when trailing. Acts as the break-even price offset in price steps. | `6` |
| `SpreadMultiplier` | Multiplier applied to the instrument price step to compute the minimal stop level distance. | `2` |
| `PositionType` | Selects which side is managed: all positions, only longs or only shorts. | `All` |

## Data requirements
* Level1 stream for the security to access best bid/ask quotes. Without bid/ask information the trailing logic cannot update.
* The security must expose `MinPriceStep` (or `PriceStep`) because all point-based parameters are translated using that value.

## Usage
1. Attach the strategy to a portfolio/security pair and make sure the security has a valid price step configured.
2. Optionally adjust the parameters above to match the desired distance and sensitivity.
3. Start the strategy and open positions manually or from another system. The strategy will attach a take profit limit order automatically and trail it as price evolves.
4. Disable the strategy or close the position to remove the trailing orders.

## Notes
* Trailing occurs only when the market moves against the current position by more than `TrailingStepPoints`. Profitable moves do not extend the take profit further away.
* The volume of the trailing order is always aligned with the current net position. If the position is closed externally the pending order is cancelled automatically.
* The strategy does not place stop-loss orders. Combine it with `StartProtection` or manual stops if additional risk management is required.
