# Scalping Assistant

The **Scalping Assistant** strategy is a direct conversion of the MetaTrader 4 expert advisor "Scalper Assistant v1.0". It does not generate entries on its own. Instead, it monitors open positions on the configured security and manages protective orders in a MetaTrader-like fashion.

## How it works

1. When a new position is detected the strategy immediately registers stop-loss and take-profit orders using the configured distances (expressed in price steps).
2. The strategy subscribes to level1 data and continuously tracks the best bid/ask to estimate the current profit of the position.
3. Once the unrealized profit reaches `BreakEvenTriggerPoints` the initial stop is cancelled and re-registered at the break-even price plus the configured offset.
4. The stop level remains at break-even; no further trailing is performed. The take-profit order is left untouched.
5. As soon as the position is closed all protective orders are cancelled and the internal state is reset, ready for the next manual trade.

## Usage notes

- Attach the strategy to a connector/portfolio and open trades manually or from another algorithm. The assistant will take over the protection of those positions.
- The logic relies on level1 quotes; make sure the selected connector provides best bid/ask updates.
- The term *points* refers to the instrument price step (`Security.PriceStep`). For forex symbols with five decimal places this equals one pip.

## Parameters

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `StopLossPoints` | `decimal` | `30` | Distance (in price steps) used when placing the initial protective stop. Set to `0` to skip sending a stop order. |
| `TakeProfitPoints` | `decimal` | `100` | Distance (in price steps) used when placing the initial take-profit order. Set to `0` to skip the take-profit. |
| `BreakEvenTriggerPoints` | `decimal` | `15` | Profit in price steps that must be reached before the stop is moved to break-even. |
| `BreakEvenOffsetPoints` | `decimal` | `5` | Extra distance (in price steps) added above/below the entry price when the stop is shifted to break-even. |

## Conversion status

- ✅ Core logic: break-even stop handling based on MetaTrader input parameters.
- ✅ High-level API usage: `SubscribeLevel1()` with delegate binding.
- ✅ Protective orders: created via `SellStop`, `BuyStop`, `SellLimit`, and `BuyLimit` helpers.
- ❌ No Python port – only the C# strategy is provided, matching the request.
