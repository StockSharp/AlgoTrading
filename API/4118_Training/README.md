# Training Strategy

## Overview
The strategy replicates the manual "Training" expert advisor from MetaTrader 4. The original script draws draggable labels that users pull above or below a threshold to request market orders. In StockSharp we expose the same controls as boolean strategy parameters. A lightweight timer polls the parameters just like the MQL `Control()` loop and submits market orders, closes positions, and refreshes the on-screen status log.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `OrderVolume` | Contracts traded by each manual request. | `1` |
| `TakeProfitPoints` | Distance in price steps used when placing the take-profit order. Set to `0` to disable. | `30` |
| `StopLossPoints` | Distance in price steps used for the protective stop order. Set to `0` to disable. | `30` |
| `RequestBuy` | Toggle to `true` to submit a market buy order. The flag is reset automatically after processing. | `false` |
| `RequestSell` | Toggle to `true` to submit a market sell order. The flag is reset automatically after processing. | `false` |
| `CloseBuy` | Toggle to `true` to exit an existing long position. The flag resets even if no long is present. | `false` |
| `CloseSell` | Toggle to `true` to exit an existing short position. The flag resets even if no short is present. | `false` |

## Trading Logic
- A 250 ms timer mirrors the MQL `Control()` procedure and drives all actions.
- When `RequestBuy` or `RequestSell` becomes `true`, the strategy cancels any leftover protection, normalises the requested volume, and sends a market order through `BuyMarket`/`SellMarket`.
- Manual close flags (`CloseBuy`, `CloseSell`) flatten the corresponding positions and wipe existing protective orders.
- Once an entry trade is filled the strategy recreates the stop-loss and take-profit orders using `Security.PriceStep` to convert points into absolute prices. If the instrument does not define a price step, the protective orders are skipped and a warning is logged.
- Filled protection orders automatically cancel their counterpart so the position is left unmanaged, exactly as in the original EA.
- Every five seconds `AddInfoLog` prints the current portfolio value, realised PnL, and net position to replace the MQL `Comment()` output.

## Conversion Notes
- Label dragging in MQL is mapped to boolean parameters because StockSharp does not provide chart label hit-testing by default.
- `OrderSend` calls map to the high-level `BuyMarket` and `SellMarket` helpers, which simplify order registration and merge hedging behaviour into a single net position.
- Stop-loss and take-profit levels are re-created via `SellStop`/`SellLimit` for long trades and `BuyStop`/`BuyLimit` for shorts, mirroring the attached orders in the original code.
- The clean-up routine `Delete_My_Obj` becomes `CancelProtectionOrders`, which cancels all outstanding protection orders whenever the position returns to zero.

## Usage Tips
1. Configure the traded instrument so that `PriceStep` (and optionally `StepPrice`) are filled; otherwise distance-to-price conversions will be skipped.
2. While the strategy is running, flip the boolean parameters from the UI or programmatically to simulate button presses.
3. Adjust `OrderVolume`, `TakeProfitPoints`, and `StopLossPoints` before submitting requests—the timer automatically respects the new values.
4. Monitor the platform log for the periodic balance and PnL summaries that replace the MQL on-chart text.
