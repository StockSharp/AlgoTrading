# Frank Ud Minimal Strategy

This sample ports the classic **Frank Ud** MetaTrader expert advisor into StockSharp using the high-level strategy API. The original MQL script runs a hedged martingale grid that keeps adding positions every time price moves against the latest entry. Profits are locked once the most recent (and therefore largest) order earns a fixed number of pips, after which *all* trades on that side are closed simultaneously.

## Core logic

1. **Symmetric hedging.** The strategy maintains two independent ladders of market positions: a long ladder and a short ladder. It is therefore possible to hold longs and shorts at the same time, as in MetaTrader's hedging mode.
2. **Martingale progression.** The first order on any side uses `InitialVolume` (default 0.1 lots). Each subsequent entry on the same side doubles the largest currently open volume. Volume adjustments respect the instrument's `MinVolume`, `MaxVolume`, and `VolumeStep` constraints.
3. **Entry spacing.** A new position is added only when price has moved by at least `ReEntryPips` (default 41 pips) beyond the best entry price of the existing ladder. The long ladder waits for ask prices to drop below `lowest_buy - ReEntryPips`, while the short ladder waits for bid prices to rise above `highest_sell + ReEntryPips`.
4. **Profit harvesting.** For each ladder the trade with the largest volume acts as the "trigger" order. When its profit exceeds `TakeProfitPips` (default 65 pips), or when price touches the implicit take-profit level `(TakeProfitPips + 25)` used by the MQL version, every position on that side is flattened with a single market order.
5. **Margin protection.** Before submitting any new entry the strategy verifies that the free margin reported by the portfolio (`CurrentValue - BlockedValue`) stays above `Balance × MinimumFreeMarginRatio` (default 0.5). If the broker does not report portfolio statistics the check falls back to the fixed-volume behaviour of the original expert.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `TakeProfitPips` | Pip profit threshold measured on the most recent, largest order. Once exceeded, all positions on that side are closed. |
| `ReEntryPips` | Minimum pip distance between the best existing entry and the current bid/ask before a new martingale order is added. |
| `InitialVolume` | Base lot size for the first order of each ladder. Subsequent orders double the largest active volume. |
| `MinimumFreeMarginRatio` | Required ratio of free margin to balance before new entries are allowed. Set to 0 to disable the check. |

## Implementation notes

- The strategy relies solely on level-1 quotes: bid updates drive the short ladder logic and ask updates drive the long ladder logic.
- Order intents are tracked in an internal dictionary so that `OnNewMyTrade` knows whether a fill opened or closed a ladder. This mimics the explicit ticket bookkeeping in the MQL source.
- Position bookkeeping stores every fill (price and volume) in lists instead of querying cumulative statistics, preserving the behaviour of the MQL arrays that were used to locate the largest lot and its entry price.
- The extra 25 pip buffer that the original expert placed on each take-profit order is retained as an additional exit condition.

> **Note:** The Python port is intentionally omitted for now, as requested. The folder contains only the C# implementation and the multilingual documentation.
