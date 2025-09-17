# Trade Arbitrage Strategy

## Overview
This strategy is a StockSharp conversion of the MetaTrader expert advisor "Trade-Arbitrage". It reconstructs synthetic foreign-exchange quotes for every ordered currency pair in the configured currency universe and looks for mispricing between any two synthetic variants. Whenever the difference between two quotes exceeds the `MinimumEdgePips` threshold, the strategy opens a market-neutral basket that buys the cheaper leg and sells the more expensive one. Each basket is decomposed into the underlying FX instruments so the resulting position is hedged across the market just like in the original MetaTrader implementation.

## Market data and subscriptions
- Resolves all tradable currency pairs using the currency list and the optional `SymbolSuffix` parameter. All resolved instruments are subscribed via `SubscribeLevel1()` to stream best bid/ask quotes.
- If the primary `Strategy.Security` contains a suffix (for example `EURUSDm`), the suffix is automatically propagated to the generated symbols when `SymbolSuffix` is left empty.
- Every Level1 update recalculates synthetic bids and asks for all arbitrage variants. The strategy immediately re-evaluates the arbitrage graph, so orders react on tick updates without polling loops.

## Trading logic
1. For each ordered currency pair (A/B) the strategy generates the same variants as the MQL version: the direct quote, the inverse quote and four synthetic cross rates built through every third currency.
2. `MinimumEdgePips` is converted into an absolute price step (`MinEdge`) that depends on the current quote size (0.0001 for prices below 10 and 0.01 otherwise). The spread advantage check matches the original expression `Bid1 - MinEdge > Ask2` / `Ask1 + MinEdge < Bid2`.
3. The `AllowedPatterns` parameter can replicate the `Trade-Arbitrage.txt` filter. When it is empty all combinations are enabled.
4. Internal state `NetPositions` mirrors the MQL arrays `XPosition`/`CountArbitrage` and prevents reopening the same opportunity before the previous basket is neutralised or reversed.
5. When an edge is found the strategy decomposes both variants into real currency legs using the same formulas as the EA and accumulates the requested volumes inside `_pendingVolumes`.

## Order and position management
- Pending volumes are normalised by each security's `VolumeStep`, `MinVolume` and `MaxVolume` and sent through `BuyMarket` / `SellMarket`. Volumes larger than `MaxLot` are split into several chunks and all calculations respect the configured `MinLot` filter.
- Every arbitrage basket uses market orders. The strategy does not leave resting limit orders and therefore avoids manual cancellation logic.
- When the arbitrage direction flips, the `NetPositions` accounting requests a double-sized basket, which closes the existing exposure before establishing the opposite one. This matches the behaviour of the original EA that accumulates hedged chains.

## Risk management notes
- The strategy does not keep rolling statistics files like the MetaTrader version, but it logs instrument resolution issues through the strategy logger.
- Always verify that the contract sizes and volume steps supplied by the brokerage match the `MinLot`/`MaxLot` inputs from the expert advisor. Incorrect values can prevent the strategy from sending orders after volume normalisation.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `Currencies` | Comma-separated list of ISO currency codes used to build synthetic rates. |
| `MinimumEdgePips` | Minimal price advantage (in pips) required before opening an arbitrage basket. |
| `LotSize` | Base basket volume that is also used when reversing an active opportunity. |
| `MinLot` | Minimal trade size after volume normalisation. |
| `MaxLot` | Maximum size for a single market order; larger baskets are split automatically. |
| `AllowedPatterns` | Optional whitelist of "Variant1 && Variant2" combinations, equivalent to `Trade-Arbitrage.txt`. |
| `SymbolSuffix` | Optional suffix appended to generated symbol codes when resolving instruments. |

## Usage tips
- Keep the Market Watch list populated with every currency pair referenced by the strategy, including cross rates. Missing symbols are reported via `LogWarning` and the corresponding arbitrage combinations are skipped.
- A fast data feed is crucial: the edge check is run on every best bid/ask update, therefore stale quotes will materially reduce the effectiveness of the strategy.
- If your broker supports direct hedging, leave `MinLot` as low as the contract specification allows so the basket decomposition can mimic the original MetaTrader fills without rounding errors.
