# Quick Trade Keys Strategy

## Overview
The **Quick Trade Keys Strategy** is a lightweight manual assistant that mirrors the behaviour of the MetaTrader script "QuickTradeKeys123". In the original MQL5 implementation, a trader could press keyboard keys to instantly submit market buy or sell orders or to close all positions opened by the script. The StockSharp port exposes the same actions as boolean strategy parameters so that they can be triggered from the strategy UI, the optimizer, or automated orchestration tools while preserving deterministic backtesting behaviour.

## Trading Logic
1. **Timer-driven command polling** – the strategy does not rely on market data. Instead, it starts an internal timer that fires every 200 milliseconds. Each tick of this timer calls an internal method that inspects the three manual command parameters.
2. **Manual command execution** – when any of the parameters is set to `true`, the strategy performs the corresponding action:
   - `BuyRequest` sends a market buy order using the configured `OrderVolume`.
   - `SellRequest` sends a market sell order using the same `OrderVolume`.
   - `CloseAllRequest` flattens the net position by issuing a market order in the opposite direction of the current exposure.
   After executing a command the strategy resets the triggering parameter back to `false`, ensuring that each toggle results in exactly one action.
3. **Connection safety checks** – before placing any orders the strategy verifies that it is online and that both `Security` and `Portfolio` are assigned. These guards reflect the original MQL script requirement that trade functions can only run when the environment is ready.
4. **Position handling** – the close-all command uses the aggregated position available through the StockSharp base `Strategy` class. This matches the MQL script behaviour, which also closed only the positions created by the script (identified by the magic number).

## Parameters
| Parameter | Description |
|-----------|-------------|
| `OrderVolume` | Volume for new manual market orders triggered by the buy or sell commands. |
| `BuyRequest` | Set to `true` to immediately send a market buy order (auto-resets to `false`). |
| `SellRequest` | Set to `true` to immediately send a market sell order (auto-resets to `false`). |
| `CloseAllRequest` | Set to `true` to close the current net position with a market order (auto-resets to `false`). |

## Differences from the MQL Version
- Keyboard shortcuts are replaced with explicit parameters so that orders can be triggered from any StockSharp-compatible UI or script.
- The strategy uses the aggregated position maintained by StockSharp instead of iterating over raw MetaTrader tickets. This produces equivalent results when only the strategy itself is opening positions.
- Order volume, buy/sell commands, and the close-all command are handled entirely with market orders for deterministic execution in simulations.
- No chart events or tick handlers are required; a timer reproduces the instant response characteristic of the keyboard-driven script.

## Usage Notes
- Assign both `Security` and `Portfolio` before starting the strategy; otherwise it throws an exception to highlight the missing configuration.
- Because actions are checked on a timer, commands execute even if the market is idle and no fresh data arrives.
- Consider combining the strategy with StockSharp UI elements (buttons, hotkeys, or automation scripts) that toggle the boolean parameters to replicate the MetaTrader keyboard workflow.
