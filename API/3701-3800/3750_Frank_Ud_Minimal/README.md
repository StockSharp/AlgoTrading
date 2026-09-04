# Frank Ud Minimal Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This sample ports the classic **Frank Ud** MetaTrader expert advisor into StockSharp using the high-level strategy API. The original MQL script runs a hedged martingale grid that keeps adding positions every time price moves against the latest entry. Profits are locked once the most recent (and therefore largest) order earns a fixed number of pips, after which *all* trades on that side are closed simultaneously.

## Core logic

1. **Symmetric hedging.** The strategy maintains two independent ladders of market positions: a long ladder and a short ladder. It is therefore possible to hold longs and shorts at the same time, as in MetaTrader's hedging mode.
2. **Martingale progression.** The first order on any side uses `InitialVolume` (default 0.1 lots). Each subsequent entry on the same side doubles the largest currently open volume. Every lot the strategy sends — the very first one included — is then clamped to what the instrument actually accepts: floored to a whole number of `VolumeStep` units, raised to `MinVolume` when it came out below it, and capped at `MaxVolume`. Constraints the instrument leaves unreported are skipped.
3. **Entry spacing.** A new position is added only when price has moved by at least `ReEntryPips` (default 41 pips) beyond the best entry price of the existing ladder. The long ladder waits for ask prices to drop below `lowest_buy - ReEntryPips`, while the short ladder waits for bid prices to rise above `highest_sell + ReEntryPips`. Both sides of the quote are taken from the same candle close, so in this port the two comparisons are made against the same price.
4. **Profit harvesting.** For each ladder the trade with the largest volume acts as the "trigger" order. When its profit exceeds `TakeProfitPips` (default 65 pips), or when price reaches the buffered target sitting `TakeProfitPips + ExtraTakeProfitPips` pips away from that entry, every position on that side is flattened with a single market order and the ladder is emptied.
5. **Margin protection.** Before submitting a new entry the strategy verifies that the portfolio's free margin — its current value minus the commission it reports — stays above `Balance × MinimumFreeMarginRatio` (default 0.5). The guard covers both ladders and every entry on them, the very first one included. Setting the ratio to zero switches it off, and so does a portfolio that reports no value at all: in either case the check simply passes and the strategy falls back to the fixed-volume behaviour of the original expert.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `TakeProfitPips` | Pip profit threshold measured on the most recent, largest order. Once exceeded, all positions on that side are closed. |
| `ReEntryPips` | Minimum pip distance between the best existing entry and the current bid/ask before a new martingale order is added. |
| `InitialVolume` | Base lot size for the first order of each ladder. Subsequent orders double the largest active volume. |
| `MinimumFreeMarginRatio` | Required ratio of free margin to balance before new entries are allowed. Set to 0 to disable the check. Default 0.5. |
| `ExtraTakeProfitPips` | Additional pip distance added to `TakeProfitPips` when the buffered exit target is computed. Default 25. |
| `CandleType` | Candle series the strategy subscribes to. Default: 1-minute time frame. |

## Implementation notes

- A pip is not the raw price step. On the first finished candle it processes, the strategy sets one pip to a ten-thousandth of the quoted price, floors it at the instrument's price step so that it can never be finer than the instrument trades, and then keeps that value for the rest of the run so the grid does not move under itself. This reproduces the forex convention the expert was written for — 0.0001 on EURUSD at 1.10, 0.01 on USDJPY at 150 — and keeps the distances meaningful on an instrument quoted in five figures, where the raw 0.01 step would clear a 65 pip target on almost every candle. If the instrument reports no price step, the fraction alone defines the pip.
- The strategy is driven by finished candles, not by level-1 quotes. It subscribes to the `CandleType` series (a 1-minute time frame by default) and ignores every candle that is not finished yet. The bundled history carries no order book, so the close of the finished candle stands for both the bid and the ask. The C# and the Python implementation subscribe in exactly the same way.
- A ladder entry is recorded at the moment the order is sent, not when it is filled: opening appends the candle close and the requested volume to the list, while closing sends a single market order for the ladder's whole volume and empties the list. No order-to-intent map is kept and no fill callback is used — in this emulator the fill is delivered synchronously inside order registration, before the order could even be written into such a map.
- Position bookkeeping stores every ladder entry (price and volume) in plain lists instead of querying cumulative statistics, preserving the behaviour of the MQL arrays that were used to locate the largest lot and its entry price.
- The extra pip buffer that the original expert placed on each take-profit order is exposed as the `ExtraTakeProfitPips` parameter (25 pips by default) and kept as an additional exit condition.

> Implementations are available in both C# and Python.
