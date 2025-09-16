# Open Two Pending Orders Strategy

## Overview
This strategy replicates the MetaTrader expert advisor that simultaneously places a buy stop and a sell stop order around the current spread. It works on a single security and uses high-level StockSharp API calls to subscribe to the order book, manage pending orders, and handle portfolio risk controls. As soon as one pending order is filled, the opposite order is cancelled and the active position is managed with stop-loss, take-profit, and trailing-stop rules.

## Trading Logic
1. Subscribe to the order book and read the best bid and ask prices.
2. When there is no open position or active entry order, calculate the entry volume and place two stop orders:
   - Buy stop at *ask + EntryOffsetPoints × PriceStep*.
   - Sell stop at *bid − EntryOffsetPoints × PriceStep*.
3. When a stop order is executed:
   - Cancel the opposite pending order.
   - Store the execution price as the new entry price.
   - Compute the initial stop-loss and take-profit levels in price steps relative to the fill.
4. While the position is active, monitor the order book:
   - Close longs when the bid reaches the stop-loss or take-profit level.
   - Close shorts when the ask reaches the stop-loss or take-profit level.
   - Activate the trailing stop after price moves in favour of the trade by the trailing distance and slide the stop level accordingly.
5. When the position returns to flat, reset the internal state and place a fresh pair of stop orders.

Exits are executed with market orders once a protective level is touched. This keeps the logic close to the MQL implementation without relying on lower-level order modification APIs.

## Money Management
The strategy can use either a fixed volume or dynamic risk-based sizing:
- **Fixed Volume** – use the constant lot size defined by the `FixedVolume` parameter.
- **Money Management** – if enabled, calculate the volume from the portfolio equity, the risk percentage, and the stop-loss distance in price steps. Volumes are rounded to the instrument volume step and clamped between the instrument’s minimum and maximum values.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `UseMoneyManagement` | Enables risk-based position sizing. Default: `true`. |
| `RiskPercent` | Percentage of portfolio equity to risk per trade when money management is active. Default: `2`. |
| `FixedVolume` | Lot size used when money management is disabled. Default: `1`. |
| `StopLossPoints` | Stop-loss distance in price steps from the entry price. Default: `100`. |
| `TakeProfitPoints` | Take-profit distance in price steps from the entry price. Default: `300`. |
| `TrailingStopPoints` | Trailing stop distance in price steps. A value of `0` disables trailing. Default: `50`. |
| `EntryOffsetPoints` | Distance in price steps used to place the pending orders away from the spread. Default: `50`. |
| `SlippagePoints` | Extra cushion in price steps reserved for slippage. Currently informational and not used directly. Default: `5`. |

## Notes
- The strategy relies on the order book feed. Ensure that market depth data is available for the selected security.
- Stop-loss and take-profit execution uses market orders once the bid/ask crosses the level, matching the behaviour of the original MQL trailing stop logic.
- Trailing stops start only after the price has moved by the configured trailing distance from the entry.
- The code uses tab indentation, English comments, and high-level StockSharp methods according to the project guidelines.
