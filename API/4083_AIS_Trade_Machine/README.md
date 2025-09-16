# AIS4 Trade Machine Strategy

## Overview
The **AIS4 Trade Machine Strategy** is a manual trading assistant that ports the original MetaTrader "AIS4 Trade Machine" expert advisor to StockSharp. It keeps the one-position workflow from the script: the operator supplies absolute stop-loss and take-profit levels, issues a command, and the strategy calculates the trade size based on the current account equity and instrument specifications. After the market order is filled the strategy immediately submits paired protective orders (stop + limit) so the requested risk and reward levels are enforced on the exchange side.

The strategy does **not** generate automatic signals. It is designed for discretionary execution where the user decides when and where to enter or modify a position.

## Manual workflow
1. Make sure the connected instrument exposes `PriceStep`, `StepPrice`, `VolumeStep`, `MinVolume`, and `MaxVolume`. They are required to convert price risk into contract size and to align the order volume with exchange limits.
2. Before sending a command, set `StopPrice` and `TakePrice` to the absolute price levels you want to use.
3. Change `Command` to `Buy` or `Sell`. The strategy:
   - Checks that no other position is open.
   - Verifies that the requested stop-loss and take-profit respect the minimum tick distance.
   - Computes the risk budget from `OrderReserve` × current portfolio equity and ensures that the equity reserve (`AccountReserve`) is respected.
   - Estimates the order volume from the stop distance and the instrument tick value.
   - Sends the market order and then submits paired protective orders (`SellStop`+`SellLimit` for longs, `BuyStop`+`BuyLimit` for shorts).
4. `Command` is automatically reset to `Wait` after the action is handled so accidental duplicate executions are avoided.

### Managing an existing position
- Set new price levels (use `0` to keep the current value) and switch `Command` to `Modify`. The strategy cancels the previous protective orders and replaces them with new ones that match the updated prices.
- Switch `Command` to `Close` to liquidate the active position at market and cancel any protective orders.

## Risk management logic
- **AccountReserve** – keeps a fraction of peak equity untouched. Trading is blocked while the available equity (`equity - peak_equity × (1 - AccountReserve)`) is smaller than the requested risk budget.
- **OrderReserve** – fraction of current equity allocated to the next trade. The budget is transformed into a contract size using the stop distance and instrument tick value (`PriceStep` × `StepPrice`).
- If the computed volume falls below `MinVolume` or violates the `VolumeStep`, the command is rejected and a warning is written to the log.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `Command` | `Wait` | Manual command to execute (`Buy`, `Sell`, `Modify`, `Close`). Automatically returns to `Wait` after handling. |
| `StopPrice` | `0` | Absolute stop-loss level. Must be below the entry price for longs and above for shorts. |
| `TakePrice` | `0` | Absolute take-profit level. Must be above the entry price for longs and below for shorts. |
| `AccountReserve` | `0.20` | Fraction of equity kept as reserve. Higher values require a larger cushion before new trades are accepted. |
| `OrderReserve` | `0.04` | Fraction of equity risked per trade. Used to calculate the contract size from the stop distance. |
| `CandleType` | `1 minute` time-frame | Candle series used to observe the latest prices for validation and logging. |

## Notes and limitations
- Only one position is supported at a time, matching the original expert advisor design.
- Commands that violate the minimal price distance, capital reserve, or volume constraints are ignored and a warning is recorded in the strategy log.
- Protective orders are replaced on every modification or new fill to keep volumes synchronized with the actual position size.
- The strategy relies on accurate market data for `PriceStep`/`StepPrice`. Instruments that do not provide these fields cannot be traded safely with this port.
