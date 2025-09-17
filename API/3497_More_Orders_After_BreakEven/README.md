# More Orders After BreakEven (StockSharp Port)

This folder contains a StockSharp C# port of the MetaTrader 4 expert advisor **"More Orders After BreakEven"** (MQL source id `35609`). The original EA repeatedly adds new long positions once previous trades have been protected at break-even. The port reproduces that ticket-based money management while integrating with StockSharp's high-level API.

## Strategy Overview

* **Market side** – long only. Every trade is a market buy order placed on the strategy's primary security.
* **Core idea** – while there are fewer open trades without break-even protection than `MaximumOrders`, the strategy buys again. When an existing trade reaches the break-even distance, its stop-loss is raised to the entry price so it no longer blocks additional entries.
* **Exit management** – each order stores its own stop-loss and take-profit levels. Stops are moved to break-even when the price advances by `BreakEvenPips`. Market sell orders close positions when the bid price touches either protection level.
* **Tick processing** – the original EA worked on every tick via `OnTick`. The port uses level 1 market data to monitor best bid/ask prices and emulates the same behaviour: each update evaluates entries, break-even rules, and potential exits.

## Parameters

| Name | Description | Default |
|------|-------------|---------|
| `MaximumOrders` | Maximum number of long trades whose stop-loss has not yet reached break-even. Once the count drops below this threshold new positions can be opened. | `1` |
| `TakeProfitPips` | Distance from entry price to the take-profit target expressed in MetaTrader pips. A value of `0` disables the take-profit. | `100` |
| `StopLossPips` | Initial distance to the protective stop in MetaTrader pips. Set to `0` to leave the position without an initial stop (the break-even rule can still protect it later). | `200` |
| `BreakEvenPips` | Profit distance (in MetaTrader pips) after which the stop-loss is lifted to the entry price. `0` means the stop moves to break-even as soon as the price exceeds the entry price. | `10` |
| `TradeVolume` | Volume submitted with every market buy order. | `0.01` |
| `DebugMode` | When enabled the strategy logs informational messages that mimic the original EA's `Comment()` output. | `true` |

All pip-based distances automatically adapt to 4/2 and 5/3 digit forex symbols by analysing the instrument's tick size and decimal precision, replicating the `points` scaling factor from the original code.

## Trading Logic

1. **Level 1 subscription** – the strategy subscribes to best bid/ask quotes. Every time both prices are known, `ProcessPrices` emulates the MQL `OnTick` loop.
2. **Order counting** – before placing a new order, the strategy counts open entries that have not yet reached break-even. This reproduces the original `OrdersCounter()` helper.
3. **Entries** – when the count is below `MaximumOrders`, a new buy market order is submitted using `TradeVolume`. The fill price is recorded and per-ticket stop/take-profit levels are initialised.
4. **Break-even update** – for each active entry the bid price is compared with the break-even trigger. Once exceeded, the stop-loss is moved to the entry price, marking the ticket as protected so it no longer contributes to the order count.
5. **Exit checks** – the bid price also drives exit detection. If it reaches the stored take-profit or drops to the stop-loss (including the break-even stop), the strategy issues a market sell order for the remaining volume of that ticket.
6. **Position tracking** – fills received through `OnOwnTradeReceived` maintain a FIFO list of entries. This reproduces MetaTrader's ticket behaviour where each order can be handled individually even though StockSharp aggregates the net position.

## Differences from the Original EA

* Only long trades are implemented because the MQL version never issued sell entries.
* Broker-side stop and take-profit orders are replaced with strategy-side monitoring and market exits. This is necessary because StockSharp does not automatically modify per-order stops in the high-level API.
* Diagnostic output uses StockSharp's logging system instead of `Comment()` text on the MetaTrader chart.

## Usage Notes

1. Attach the strategy to a connector that supplies level 1 data for the chosen security.
2. Configure the pip-based parameters to match the instrument's volatility and broker requirements.
3. Enable `DebugMode` during testing to verify order counting and break-even behaviour, then disable it in production for quieter logs.
4. Because exits are handled via market orders, ensure the portfolio has enough available buying power to cover all additional entries that can be triggered once break-even protection kicks in.

## Source Reference

* Original MQL4 file: `MQL/35609/More Orders After BreakEven.mq4`.
* Converted C# strategy: `CS/MoreOrdersAfterBreakEvenStrategy.cs`.
