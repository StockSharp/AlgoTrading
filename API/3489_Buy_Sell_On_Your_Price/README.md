# BuySellOnYourPrice Strategy

## Overview
- Converts the MetaTrader expert advisor **BuySellonYourPrice.mq5** (id 35391) to the StockSharp high-level API.
- Sends exactly one order on start, matching the original logic that requires no active orders or positions.
- Supports market, limit, and stop entries with optional stop-loss and take-profit levels expressed as absolute prices.
- Automatically configures StockSharp protective orders when valid stop-loss / take-profit distances can be derived from the provided price levels.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `Mode` | Order type to submit (None, Buy, Sell, BuyLimit, SellLimit, BuyStop, SellStop). | `None` |
| `OrderVolume` | Volume for the generated order. | `1` |
| `EntryPrice` | Price used for pending orders; ignored for market orders. | `0` |
| `StopLossPrice` | Absolute price level for the stop-loss. | `0` |
| `TakeProfitPrice` | Absolute price level for the take-profit. | `0` |

## Trading Logic
1. When the strategy starts it checks that:
   - A valid `Mode` different from `None` is selected.
   - `OrderVolume` is positive.
   - There is no current position and no active orders. If either is present, the order is not sent (same as `OrdersTotal()==0` and `PositionsTotal()==0` check in MQL).
2. The entry price is resolved:
   - Market modes use the best bid/ask, falling back to last price or `EntryPrice` when no market data is available yet.
   - Pending modes require `EntryPrice > 0`.
3. Protective distances are derived from the specified stop-loss and take-profit levels. Only valid, positive distances are passed to `StartProtection` to emulate the EA parameters.
4. The selected order type is sent (`BuyMarket`, `SellLimit`, `BuyStop`, etc.) exactly once and informational logs are produced to reflect the action.

## Differences from the Original EA
- Logging is performed through `AddInfoLog` instead of `Print`.
- Protective orders are registered via `StartProtection` when both the entry price and the stop-loss/take-profit allow computing a positive distance.
- Market price resolution uses current Level1 data (`BestBid`, `BestAsk`, `LastPrice`) and postpones order submission if no quote is available yet.

## Usage Notes
- Assign the desired security before starting the strategy and ensure Level1 data is available for market orders.
- Set `EntryPrice`, `StopLossPrice`, and `TakeProfitPrice` in absolute terms when using pending orders.
- Leave `Mode` as `None` to disable trading without removing the strategy from the environment.
