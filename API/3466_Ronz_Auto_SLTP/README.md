# Ronz Auto SLTP Strategy

## Overview

The **Ronz Auto SLTP Strategy** is a direct C# port of the MetaTrader 5 utility *Ronz Auto SLTP*. It acts as a trade manager that automatically attaches protective stop-loss and take-profit levels, applies profit locking, and activates trailing rules for every open position. The conversion relies on the high-level StockSharp API and supports both account-wide supervision and single-symbol deployment.

Key capabilities:

- Apply server-side or virtual (client-side) protection depending on the `UseServerStops` flag.
- Set initial stop-loss and take-profit distances using MetaTrader-style pip measurements.
- Lock in a fixed amount of profit after the trade reaches a configurable threshold.
- Execute three trailing stop variations (classic, step distance, step-by-step) mirroring the original advisor.
- Monitor all securities in the connected portfolio or restrict management to the strategy security only.
- Issue optional log notifications whenever a virtual stop or take-profit closes a position.

## Parameters

| Parameter | Default | Description |
| --- | --- | --- |
| `ManageAllSecurities` | `true` | Monitor every open position in the portfolio. Disable to manage only the strategy security. |
| `TakeProfitPips` | `550` | Distance in MetaTrader pips added to the entry price for the take-profit target (including broker minimal stop distance). |
| `StopLossPips` | `350` | Distance in MetaTrader pips subtracted from the entry price for the stop-loss level (including broker minimal stop distance). |
| `UseServerStops` | `true` | When enabled, send stop and limit orders to the broker. When disabled, close positions virtually once thresholds are hit. |
| `EnableLockProfit` | `true` | Enable the profit-lock logic that moves the stop above/below the entry price after a threshold is reached. |
| `LockProfitAfterPips` | `100` | Profit (in pips) that must be achieved before the lock logic becomes active. Set to zero to skip the lock stage and trail immediately. |
| `ProfitLockPips` | `60` | Profit preserved once the lock engages. The stop is moved to entry price plus/minus this distance. |
| `TrailingStopMode` | `Classic` | Trailing algorithm used after the lock threshold. Options: `None`, `Classic`, `StepDistance`, `StepByStep`. |
| `TrailingStopPips` | `50` | Trailing distance in pips. Acts as the main buffer for both classic and step-based trailing modes. |
| `TrailingStepPips` | `10` | Increment used by step-based trailing modes. Ignored by the classic trailing variant. |
| `EnableAlerts` | `false` | When true, write log messages whenever a virtual stop or take-profit closes an order. |

## Behaviour Details

1. **Initial Protection**
   - When a new position is detected, the strategy calculates stop-loss and take-profit targets relative to the entry price.
   - Broker-defined minimal stop distances are honoured by reading stop/freeze level fields from Level1 updates and expanding the requested distances if necessary.

2. **Profit Locking**
   - Once the current profit exceeds `LockProfitAfterPips`, the stop is raised (or lowered for shorts) to lock `ProfitLockPips` worth of profit.
   - If locking is disabled, the strategy skips this stage and waits for the trailing conditions.

3. **Trailing Stops**
   - `Classic`: keeps a fixed distance (`TrailingStopPips`) to the current price.
   - `StepDistance`: reduces the distance by `TrailingStepPips` once the price has moved favourably enough, closely matching the MetaTrader "step keep distance" implementation.
   - `StepByStep`: pushes the stop forward in discrete `TrailingStepPips` increments once the price has advanced by the configured trailing distance.
   - Trailing begins immediately when `LockProfitAfterPips` is zero. Otherwise it activates once the profit exceeds `LockProfitAfterPips + TrailingStopPips`.

4. **Virtual Mode**
   - When `UseServerStops` is false the strategy does not register any stop/limit orders. Instead, it closes the open position via market orders as soon as the computed stop-loss or take-profit is breached.
   - Alerts can be enabled to document these virtual closures in the log.

5. **Multi-Security Support**
   - With `ManageAllSecurities = true`, the strategy subscribes to Level1 data for every security that has an open position in the selected portfolio.
   - Each security maintains its own stop, take-profit, and trailing state so that long and short trades are supervised independently.

## Usage Tips

- Attach the strategy to a portfolio and, optionally, assign a default security when only one instrument needs supervision.
- Ensure Level1 data (best bid/ask) is available for every managed symbol so that pip calculations stay accurate.
- Review broker stop level restrictions: the strategy already expands requested distances, but extremely tight configurations can still be rejected by the trading venue.
- Virtual mode is useful on brokers that do not support protective orders or during backtesting scenarios.

## Differences from the Original Expert

- StockSharp aggregates positions by security, while MetaTrader hedging mode tracks individual tickets. The port therefore manages the net position per instrument.
- The test-order functionality of the MQ5 script (opening dummy trades in the tester) was intentionally omitted.
- Alerts are delivered through the StockSharp logging subsystem rather than on-screen pop-ups.

