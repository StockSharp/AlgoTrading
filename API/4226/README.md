# Viva Las Vegas Strategy

## Overview
Viva Las Vegas is a playful money-management expert that randomly buys or sells the attached instrument and then lets one of five staking systems decide the size of the next wager. The StockSharp port keeps the original MetaTrader behaviour by:
- Choosing a trade direction through a pseudo-random coin toss on every new attempt.
- Immediately placing symmetrical stop-loss and take-profit protections expressed in pips.
- Updating the progression sequence as soon as the previous position is closed and opening a fresh position right away.

The strategy therefore stays constantly exposed (one open position at a time) and showcases how several classic betting systems behave inside StockSharp’s trading framework.

## Money-management modules
The `MoneyManagement` parameter selects one of the following staking models, all of which use `BaseVolume` as their anchor lot size:

1. **Martingale** – double the lot size after every losing trade and reset to the base volume after a profitable trade.
2. **Negative Pyramid** – double the lot size after a loss, but cut the volume in half after a win (never going below the base volume).
3. **Labouchere** – maintain a numeric sequence (default `1-2-3`), stake the sum of the first and last numbers, remove them after a win, and append their sum after a loss.
4. **Oscar’s Grind** – increase the bet by the base lot after each win until one base lot of profit has been accumulated, then reset; losses only decrease the running result.
5. **31 System** – cycle through the series `1,1,1,2,2,4,4,8,8`, doubling the current element after the first win and resetting to the beginning after the second consecutive win.

All modules closely follow the original MQL implementation, including how volume progressions react to ties (zero-profit trades are treated as losses).

## Trading workflow
1. On start the strategy seeds the pseudo-random generator (time-based when `Seed = 0`) and enables StockSharp’s protective engine with symmetric stops and targets.
2. When no position is open and no order is pending, the strategy asks the active staking module for the next lot size, rounds it to the instrument’s `VolumeStep`, and tosses a coin to choose between `BuyMarket` and `SellMarket`.
3. Once the position is established, the protective module manages the exit using the configured pip distance.
4. When the position returns to flat, the realized PnL delta is evaluated:
   - Profit &gt; 0 → the module receives a **win** notification.
   - Profit ≤ 0 → the module receives a **loss** notification.
5. The process loops immediately, so the account is always either in a trade or waiting for a fresh fill.

Because only one position exists at any time, the strategy is easy to follow on a chart and perfectly mirrors the single-ticket behaviour of the original expert advisor.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `StopTakePips` | `int` | `50` | Distance (in pips) applied to both stop-loss and take-profit orders via `StartProtection`. |
| `BaseVolume` | `decimal` | `1` | Anchor lot size fed into the money-management progression. |
| `MoneyManagement` | `MoneyManagementMode` | `Martingale` | Staking algorithm controlling how the next order size is calculated. |
| `Seed` | `int` | `0` | Pseudo-random generator seed. A value of zero switches to a time-dependent seed so every run differs. |

## Implementation notes
- Volumes are normalized to the instrument’s `VolumeStep` and checked against `MinVolume` / `MaxVolume` to avoid rejected orders.
- Stop/take distances are converted to price steps using the classic MetaTrader rule (`Digits` equal to 3 or 5 implies ten ticks per pip).
- Realized profit is measured via the strategy’s `PnL` property, ensuring that protective exits and manual closes influence the staking sequence exactly like in the original code.
- English inline comments highlight the decision points, making it easy to adapt the template for educational purposes or controlled risk experiments.

## Usage tips
- Pick a demo connector or replay environment; the algorithm is intentionally risky and meant for experimentation.
- Adjust `BaseVolume` to match the instrument’s contract size before starting the strategy.
- Combine the strategy with StockSharp charts to watch how each staking system escalates or contracts the position size over time.
