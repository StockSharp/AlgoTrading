# Trailing Stop Manager
[Русский](README_ru.md) | [中文](README_cn.md)

Port of the MetaTrader expert advisor **Exp_TrailingStop**. The strategy manages trailing stops for positions that are opened by other systems and does not create entries by itself.

## How it works

- Listens to Level1 updates to track the latest best bid and ask quotes.
- When a long position is open and the ask price has moved in profit by `TrailingStartPoints` price steps, the stop-loss is recalculated to `Ask - StopLossPoints * PriceStep`.
- The long trailing stop is only advanced when the new level is at least `TrailingStepPoints` price steps higher than the previous stop.
- When a short position is open and the bid price has moved in profit by `TrailingStartPoints` price steps, the stop-loss is recalculated to `Bid + StopLossPoints * PriceStep`.
- The short trailing stop is only advanced when the new level is at least `TrailingStepPoints` price steps lower than the previous stop.
- If the best quote crosses the active trailing stop, the strategy closes the entire position at market and resets its internal state.
- Trailing logic is reset whenever the net position becomes flat or the direction flips.

## Parameters

- `StopLossPoints` (default **1000**) – distance in price steps between the market price and the trailing stop.
- `TrailingStartPoints` (default **1000**) – profit distance in price steps required before trailing is activated.
- `TrailingStepPoints` (default **200**) – minimal improvement in price steps before the stop is moved again.
- `PriceDeviationPoints` (default **10**) – reserved parameter kept for parity with the original MQL expert. It is not used because StockSharp handles order slippage differently.

All parameters are exposed through `StrategyParam<T>` so they can be optimized or adjusted in the UI.

## Additional notes

- Requires a valid `Security` with a positive `PriceStep` and Level1 data feed.
- Works with any timeframe because it reacts only to bid/ask changes.
- Designed to be combined with other entry strategies or manual trading.
- The implementation stores only the latest trailing level without keeping historical collections, mirroring the lightweight behavior of the MQL script.
- The strategy calls `SellMarket`/`BuyMarket` to exit positions because StockSharp recalculates protective orders internally, eliminating the need to send modification requests with deviations.
