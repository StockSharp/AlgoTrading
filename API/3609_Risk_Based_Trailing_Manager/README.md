# Risk Based Trailing Manager Strategy

## Overview
The **Risk Based Trailing Manager Strategy** is a risk-management assistant converted from the MetaTrader 5 script located at `MQL/44099/test.mq5`. The StockSharp version continuously supervises the current position on the assigned security, measuring floating profit and loss against the account balance. When the position reaches configurable percentage thresholds or violates a virtual trailing stop, the strategy exits the market to preserve capital.

Unlike the original MQL code that directly modifies broker-side stop-loss orders, this implementation keeps the trailing stop logic inside the strategy. As soon as price action crosses the computed trailing level, the strategy closes the position using market orders. This behaviour matches the intent of the source script while keeping the solution compatible with StockSharp's high-level API.

## How it works
1. On start the strategy subscribes to the configured candle series (1 minute by default) and stores the current portfolio value.
2. For every finished candle the latest price is compared with the entry price and position volume to estimate the floating profit or loss.
3. If floating profit exceeds the profit percentage or falls below the risk percentage (both relative to the balance), the position is closed with a market order.
4. While the position remains open, the strategy maintains virtual trailing stop levels:
   - Long positions trail below price after it moves upwards, never rising above the entry price.
   - Short positions trail above price after it moves downwards, never falling below the entry price.
5. When the trailing level is breached by candle extremes, the strategy exits the position immediately.

The trailing logic mimics the MQL version's `PositionModify` behaviour by recalculating the stop distance in points and storing it internally.

## Parameters
| Name | Description |
| ---- | ----------- |
| **Risk Percentage** | Percentage of the current portfolio value tolerated as floating loss before closing. |
| **Profit Percentage** | Percentage of the current portfolio value targeted as floating profit to secure gains. |
| **Trailing Stop Points** | Distance in price points used for the trailing stop calculation. Set to zero to disable trailing. |
| **Candle Type** | Candle data source used to drive periodic evaluations (defaults to 1-minute time frame). |

## Notes
- Works with a single security assigned to the strategy.
- Uses `StartProtection()` to make sure emergency portfolio protection is active.
- Does not create or manage pending orders; it only closes the current net position with market orders.

## Conversion details
- Reimplemented the percentage-based profit and loss thresholds as direct closings instead of iterating over individual tickets.
- Translated the trailing stop calculation into a virtual stop level that triggers market exits when breached.
- Added guards for missing portfolio data and zero price steps to ensure the strategy remains robust in StockSharp environments.
