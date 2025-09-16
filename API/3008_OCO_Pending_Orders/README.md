# OCO Pending Orders Strategy

## Overview
The **OCO Pending Orders Strategy** replicates the behaviour of the MetaTrader4 expert advisor `OCO_EA.mq4` inside the StockSharp high-level API. The algorithm allows a trader to arm up to four independent price triggers (buy limit, buy stop, sell limit, sell stop). Whenever the live best bid or best ask touches the configured price level the strategy sends an immediate market order, optionally cancelling all other pending triggers in a classic "one-cancels-the-others" (OCO) fashion.

The strategy relies purely on level-1 market data – no historical indicators are required. It is intended for discretionary or semi-automated trading workflows where traders manually define price levels and want the platform to execute as soon as the level is hit, while also attaching protective exit orders.

## Trading logic
1. The trader sets any combination of the four trigger prices and toggles the **Armed** parameter to `true`.
2. The strategy subscribes to level-1 updates and keeps the latest best bid and best ask in memory.
3. On every update it compares the stored prices with the configured thresholds:
   - If the best ask is *less than or equal to* the **Buy limit** price, a market buy order with the configured volume is submitted.
   - If the best ask is *greater than or equal to* the **Buy stop** price, a market buy order is submitted.
   - If the best bid is *greater than or equal to* the **Sell limit** price, a market sell order is submitted.
   - If the best bid is *less than or equal to* the **Sell stop** price, a market sell order is submitted.
4. After every executed trigger the corresponding level is cleared (set back to zero). When **Use OCO link** is enabled all other levels are cleared immediately, mirroring the original MT4 behaviour. When the OCO link is disabled other levels remain active until they trigger or are manually cleared.
5. If all trigger prices are zero the strategy automatically disarms itself by switching **Armed** back to `false`.

All executions are performed with `BuyMarket` and `SellMarket` calls to ensure immediate fills that respect the exchange routing configured in the StockSharp environment. Informative log entries are produced for every trigger to simplify monitoring.

## Parameters
- **Order volume** – volume sent with each market order. The value must be positive.
- **Buy limit price** – ask price threshold that activates a limit-style long entry. Set to `0` to disable.
- **Buy stop price** – ask price threshold that activates a stop-style long entry. Set to `0` to disable.
- **Sell limit price** – bid price threshold that activates a limit-style short entry. Set to `0` to disable.
- **Sell stop price** – bid price threshold that activates a stop-style short entry. Set to `0` to disable.
- **Stop loss (pips)** – distance in instrument points for the protective stop. Converted to price by multiplying with `Security.PriceStep` (fallback `1` when the instrument does not report a tick size).
- **Take profit (pips)** – distance in instrument points for the profit target. The same conversion logic as for the stop loss is used.
- **Use OCO link** – if `true`, the first filled order clears the remaining price levels and disarms the strategy. If `false`, remaining levels stay active until triggered individually.
- **Armed** – safety switch that enables or disables trading logic. The strategy automatically resets it to `false` whenever no active trigger levels remain.

## Risk management
`StartProtection` is enabled during `OnStarted`, attaching absolute-price stop-loss and take-profit offsets to every open position. The offsets are derived from the **Stop loss (pips)** and **Take profit (pips)** parameters using the instrument tick size. Protective orders are always sent as market orders to guarantee exit execution even when the underlying instrument is illiquid.

Because the strategy is purely event-driven it does not maintain pending limit orders on the exchange; it reacts to market data and sends market orders, just like the original MQL version that issued immediate orders and then modified them to apply stop-loss and take-profit distances.

## Usage tips
1. Configure the security, portfolio, and connection inside StockSharp as usual.
2. Set **Order volume** to match the desired lot size.
3. Enter any subset of trigger prices and flip **Armed** to `true`. Values left at `0` are ignored.
4. Optionally disable **Use OCO link** to keep remaining triggers active after the first fill.
5. Monitor the log for messages confirming each trigger and the automatic reset state.

Remember that the strategy uses the broker-provided price step. If the trading instrument quotes in fractional pips or uses unconventional tick sizes, adjust the pip-based distances accordingly before arming the strategy.

## Differences from the original MQL script
- The strategy uses StockSharp's `StartProtection` helper instead of manually modifying orders to apply stop-loss and take-profit levels.
- Level-1 data subscriptions are handled through high-level bindings instead of manual polling of `Bid`, `Ask`, `High`, and `Low` values.
- Parameters are exposed through `StrategyParam<T>` so they can be adjusted and optimised directly in the StockSharp UI.
- Logging replaces the MT4 `Comment` and `PlaySound` notifications, providing execution transparency within StockSharp logs.
