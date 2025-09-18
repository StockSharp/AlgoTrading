# eKeyboardTrader Strategy

## Overview
This strategy replicates the behaviour of the MetaTrader "eKeyboardTrader" expert advisor using the StockSharp high-level API. The original script listened for keyboard shortcuts to submit manual market orders and displayed helper text directly on the chart. In the StockSharp version the interactive inputs are exposed as strategy parameters while the execution logic, safety checks, and order protection features remain faithful to the MQL implementation.

## Trading Logic
1. **Level1 subscription** – the strategy subscribes to Level1 market data to receive the latest best bid and ask prices. These quotes are required before a manual request can be executed, mimicking the MetaTrader dependency on current tick data.
2. **Manual commands** – three boolean parameters (`BuyRequest`, `SellRequest`, `CloseRequest`) represent the original keyboard shortcuts (B, S, and C). When any parameter is set to `true` the strategy performs the corresponding market action and immediately resets the flag.
3. **Rate limiting** – a one second cooldown protects against accidental double submissions, identical to the timer check implemented in the MQL version. Requests raised during the cooldown wait for the next processing cycle.
4. **Order protection** – optional stop-loss and take-profit distances, expressed in MetaTrader points, are translated to absolute prices using `Security.PriceStep`. When at least one protective distance is configured the strategy enables StockSharp's built-in `StartProtection` logic so that every manual entry automatically receives the configured protective orders.
5. **Slippage awareness** – the `SlippagePoints` parameter is preserved for compatibility and is mentioned in the log whenever a manual order is sent, emulating the informational comments shown by the expert advisor.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `OrderVolume` | Base volume for manual market orders. |
| `StopLossPoints` | Distance from the entry price to the protective stop in MetaTrader points. Set to `0` to disable. |
| `TakeProfitPoints` | Distance from the entry price to the protective target in MetaTrader points. Set to `0` to disable. |
| `SlippagePoints` | Informational slippage tolerance displayed in the log for each manual order. |
| `BuyRequest` | Set to `true` to send a market buy order (auto-resets after processing). |
| `SellRequest` | Set to `true` to send a market sell order (auto-resets after processing). |
| `CloseRequest` | Set to `true` to flatten the net position at market price (auto-resets after processing). |

## Differences from the MQL Version
- The on-chart text prompts and sound notifications are not reproduced. Instead, logging messages document the performed actions.
- Protective orders are managed through StockSharp's `StartProtection` helper, which submits market orders when the threshold is hit instead of modifying individual MetaTrader tickets.
- Keyboard input is replaced by parameter toggles. Any UI that hosts the strategy can map user interactions (keyboard, buttons, scripts) to these parameters.
- The MetaTrader trade request diagnostics are condensed into logging statements to keep the conversion lightweight.

## Usage Notes
- Assign both `Security` and `Portfolio` before starting the strategy; these checks mirror the initialization conditions from the expert advisor.
- The manual command flags are evaluated when new Level1 data arrives. In a quiet market, actions execute on the next available quote.
- Adjusting `StopLossPoints` or `TakeProfitPoints` while the strategy is running requires restarting it to reconfigure the protective module, matching the once-per-session protection setup of the original script.
