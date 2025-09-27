# Open Time Daily Window Strategy

## Overview
The strategy reproduces the behaviour of the MetaTrader expert "OpenTime". It places market orders at a configurable time of day, optionally closes all exposure during a dedicated exit window, and applies simple money-management rules such as fixed stop-loss, take-profit, and trailing protection. The port uses the high-level StockSharp `Strategy` API, so the strategy can be combined with other components inside the framework.

## How it works
1. Every finished candle from the selected timeframe triggers a time-of-day check.
2. When the current time falls inside the trading window, the strategy sends market orders for every enabled direction:
   * If only one side is enabled, the current net position is extended or reversed until the requested volume is reached.
   * When both sides are enabled, buy and sell orders are issued in the same window. Because StockSharp accounts exposure netted by side, opening the second direction automatically offsets the opposite exposure before establishing the new one.
3. While the closing window is active, the strategy calls `ClosePosition()` once to flatten any outstanding exposure.
4. Optional stop-loss, take-profit, and trailing stop distances are delegated to `StartProtection`, which manages the protective orders using market exits.

## Parameters
- **Enable Close Window** – mirrors the `TimeClose` flag. When enabled, `Close Position Time` and `Window Length` define when existing trades are closed.
- **Close Position Time** – daily time at which the exit window begins (default 20:50).
- **Trading Time** – daily time when new trades are allowed (default 18:50).
- **Window Length** – duration of both the trading and closing windows (default 5 minutes, corresponding to the original `Duration` input).
- **Allow Sell Entries** – corresponds to the MQL `Sell` switch; enables short entries (default true).
- **Allow Buy Entries** – corresponds to the MQL `Buy` switch; enables long entries (default false).
- **Order Volume** – target net volume for each new trade (default 0.1 lots). The strategy adds the absolute value of the current position when an opposite signal appears, so reversals occur in a single market order.
- **Stop-Loss Points** – distance in points for the protective stop (default 0 disables the stop).
- **Take-Profit Points** – distance in points for the profit target (default 0 disables the target).
- **Use Trailing Stop** – enables the trailing stop logic from the original `SimpleTrailing` helper.
- **Trailing Stop Points** – trailing distance expressed in points (default 300).
- **Trailing Step Points** – additional progress required before advancing the trailing stop (default 3).
- **Candle Type** – timeframe used for the time checks (default 1-minute candles).

## Notes
- The point size is derived from the security price step. For three- and five-decimal quotes the step is multiplied by 10, reproducing the pip handling used by the MQL script.
- `StartProtection` attaches protective stops only when at least one of the distances is greater than zero. If trailing is active without a fixed stop-loss, the trailing distance is supplied as the initial protective value.
- The strategy intentionally does not manage pending orders or repeated retries, because StockSharp already provides automatic error handling for market orders.
