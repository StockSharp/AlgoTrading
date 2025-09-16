# Price Impulse Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Price Impulse strategy scans raw Level1 quotes and reacts to sudden changes between the best bid and best ask. It mirrors the original MetaTrader 5 expert advisor by watching price jumps over a configurable number of ticks and entering the market when the move exceeds a point-based threshold. Protective stop loss and take profit offsets are applied automatically through the high level `StartProtection` helper.

The approach is market-neutral: a long position opens when the ask price surges upward compared to an older quote, while a short position opens when the bid collapses below its previous value. A configurable cooldown keeps the strategy from re-entering immediately after a trade, just like the MQL implementation that waits for a specified sleep interval.

## How it Works

- Subscribes to Level1 data and stores rolling histories of best bid and best ask prices.
- Computes the price difference between the latest quote and the quote that arrived `HistoryGap` ticks earlier (with extra buffering defined by `ExtraHistory`).
- Opens a long position when the ask price rises by more than `ImpulsePoints * PriceStep` and no long exposure exists.
- Opens a short position when the bid price drops by more than the same threshold and no short exposure exists.
- Applies fixed take profit and stop loss levels expressed in price points and enforces a `CooldownSeconds` pause between orders.

## Parameters

- **OrderVolume** – volume sent with every market order. Defaults to `0.1` lots to match the source robot but can be optimized for other instruments.
- **StopLossPoints** – distance from the entry price to the protective stop, measured in instrument points. A value of `0` disables the stop.
- **TakeProfitPoints** – distance to the take profit target, also measured in points. A value of `0` disables the target.
- **ImpulsePoints** – minimum price impulse, in points, that must be exceeded between the current quote and the quote `HistoryGap` ticks back to trigger an entry.
- **HistoryGap** – number of Level1 updates separating the current price from the comparison baseline. Higher values require larger lookbacks, which smooths noise but delays entries.
- **ExtraHistory** – additional Level1 samples retained in the rolling buffer to absorb bursts of quotes when several ticks arrive between callbacks. Keeps the logic consistent with the MT5 implementation that over-samples the history array.
- **CooldownSeconds** – minimum waiting time after any trade before another entry can be placed. Ensures the strategy mirrors the MQL expert’s `InpSleep` parameter and prevents rapid flip-flopping.

## Notes

- The point distance parameters are automatically converted using `Security.PriceStep` (or `Security.MinPriceStep` as a fallback), so the same configuration adapts to different tick sizes.
- Trading only begins once the strategy is online, the history buffers contain enough data, and the impulse condition is satisfied.
- Because decisions are taken on raw quote updates, the strategy works best on liquid instruments with reliable Level1 feeds.
- There is no Python port for this strategy. Only the C# version is provided, matching the user request.
