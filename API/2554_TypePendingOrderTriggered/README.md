# Type Pending Order Triggered Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Utility strategy that mirrors the MetaTrader script *TypePendingOrderTriggered.mq5*. It does not place orders on its own. Instead,
it listens for the strategy's own filled trades and reports which type of pending order (Buy Limit, Sell Limit, Buy Stop, or Sell
Stop) produced the execution.

## Overview

- **Goal**: identify the pending order type responsible for each execution when multiple order types are working simultaneously.
- **Instruments**: works with any security supported by StockSharp because it reads information from `MyTrade.Order`.
- **Order Types Covered**:
  - Buy Limit and Sell Limit (`OrderTypes.Limit` + order side).
  - Buy Stop and Sell Stop (`OrderTypes.Conditional` + order side).
  - Any other type triggers a warning so you know the fill came from a non-pending order.
- **Output**: information messages are emitted through `AddInfoLog`. You will see them in the strategy logs, terminal output, or any
  connected log sink.

## Behaviour Details

1. When the strategy starts, it simply waits for own trades. No market data subscriptions are required.
2. Each incoming `MyTrade` is inspected once. The internal set `_reportedOrders` prevents duplicate messages when a single order
   produces multiple partial fills.
3. The method determines the order ticket (preferring the exchange-assigned identifier when available, otherwise the transaction ID).
4. Using the order side (`Sides.Buy`/`Sides.Sell`) plus `Order.Type`, the strategy resolves the text description and prints an
   English message identical to the MetaTrader version: *"The pending order {ticket} is found! Type of order is ..."*.
5. If the order was not a pending one (for example, a market order), the strategy logs a warning mirroring the original behaviour.
6. Missing order references are also reported as warnings to ease troubleshooting.

## Practical Usage

- Run this strategy alongside manual or automated logic that actually submits pending orders. The helper will record the first trade
  for every order and classify it.
- Because the strategy never calls `BuyMarket`/`SellMarket`, you can attach it to an existing connector without risk of unwanted
  trades.
- The output is especially useful when analysing execution reports or when migrating EAs from MetaTrader to StockSharp and you need
  confidence in how pending orders are handled by the broker.
- Resetting or stopping the strategy clears the cache of reported order IDs, so a restarted session will report new fills again.

## Parameters

This port does not introduce configurable parameters. All behaviour is fixed to reproduce the original MQL logic as closely as
possible.

## Migration Notes

- MetaTrader's `HistoryOrderSelect` loop is no longer necessary because StockSharp provides a direct `Order` reference inside
  `MyTrade` objects.
- Instead of printing via `Print`, the strategy uses StockSharp logging helpers (`AddInfoLog`/`AddWarningLog`). Hook them up to your
  preferred logging destination for best visibility.
