# Close At Time Strategy

This utility strategy automatically shuts down open activity at a user-defined local time.
It can cancel pending orders and close market positions based on several filters.

## Parameters

- `CloseAll` – if enabled, every active order and position is closed.
- `CloseBySymbol` – close only items where the security code matches `SymbolToClose`.
- `CloseByMagicNumber` – close orders whose `UserOrderId` equals `MagicNumber`.
- `CloseByTicket` – close the specific order with identifier `TicketNumber`.
- `ClosePendingOrders` – cancel working limit/stop orders.
- `CloseMarketOrders` – close open positions using market orders.
- `TimeToClose` – local moment when the closing procedure starts.
- `SymbolToClose` – security code filter for `CloseBySymbol`.
- `MagicNumber` – expected value of `Order.UserOrderId`.
- `TicketNumber` – expected value of the order identifier.

## Logic

When started, the strategy schedules a one-off task for `TimeToClose`.
Once the time is reached, it performs the following steps:

1. Iterates over all active orders and checks each against the selected filters.
2. Matching pending orders are cancelled.
3. If `CloseMarketOrders` is true, positions satisfying the filters are closed with market orders.

The approach mirrors the original MQL script which closed orders at a specified time,
but it is implemented using StockSharp's high-level API.

## Notes

- If `TimeToClose` is in the past, closing begins immediately.
- Filters are combined with logical OR; enabling `CloseAll` overrides all other filters.
- Magic and ticket numbers are treated as string values due to platform differences with MQL.
