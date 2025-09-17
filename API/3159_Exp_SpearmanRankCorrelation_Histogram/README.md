# Exp Spearman Rank Correlation Histogram Strategy

This StockSharp strategy ports the MetaTrader expert **Exp_SpearmanRankCorrelation_Histogram**. It subscribes to a configurable candle series, calculates the Spearman rank correlation histogram for each completed bar, and reacts when the color-coded state changes. Depending on the trade mode the algorithm can close opposite positions, reverse into a new trade, or wait for extreme values before acting.

## Indicator pipeline

1. A `RankCorrelationIndex` indicator (Spearman rank correlation scaled to ±100) is fed with candle closing prices. The lookback window is bounded by `MaxRange` and defaults to 14 bars.
2. The raw correlation is normalised to the `[-1, 1]` interval. When `InvertCorrelation` is enabled the sign is flipped to emulate the MQL `direction` flag.
3. The normalised value is compared with `HighLevel` and `LowLevel` to assign a colour state:
   * `4` – strong bullish zone (`value > HighLevel`).
   * `3` – moderate bullish zone (`0 < value ≤ HighLevel`).
   * `2` – neutral (`value == 0`).
   * `1` – moderate bearish zone (`LowLevel ≤ value < 0`).
   * `0` – strong bearish zone (`value < LowLevel`).
4. The latest colours are stored in a series-style buffer so that index `0` represents the most recent closed candle, index `1` the previous one, and so on.

## Trading workflow

* Signals are evaluated only on finished candles (`CandleStates.Finished`).
* The `SignalBar` parameter defines which completed bar is inspected (default one bar back). The strategy also looks at the immediately older bar, replicating the double-buffer lookup from the expert advisor.
* Order toggles (`AllowBuyEntries`, `AllowSellEntries`, `AllowBuyExits`, `AllowSellExits`) decide whether long/short positions may be opened or closed.
* Trade modes reproduce the MetaTrader switch:
  * **Mode 1** – close the opposite position whenever the older colour is bullish/bearish (`> 2` or `< 2`). If allowed, open in the new direction when the recent colour leaves the bullish (`< 3`) or bearish (`> 1`) zone.
  * **Mode 2** – react only to extreme colours. Bullish extreme (`4`) lets the strategy close shorts and optionally open longs when the newer bar drops below `4`. Bearish extreme (`0`) closes longs and can open shorts when the newer bar rises above `0`.
  * **Mode 3** – a stricter version of Mode 2: shorts are closed immediately on `4`, longs on `0`, and new trades are allowed under the same conditions as Mode 2.
* `CancelActiveOrders()` is executed before sending new market orders to avoid stale requests.
* Position reversals use the configured `Volume` plus the absolute current position so that the trade fully flips to the opposite side.
* Optional `StopLossPoints` and `TakeProfitPoints` (price units) enable `StartProtection` based risk management; when left at `0` no protective orders are spawned.

## Parameters

| Parameter | Description |
| --- | --- |
| `CandleType` | Timeframe used for the indicator and trading decisions. |
| `RangeLength` | Nominal Spearman lookback period (capped by `MaxRange`). |
| `MaxRange` | Upper bound for the effective lookback length; falls back to `10` if set to `0`. |
| `HighLevel`, `LowLevel` | Thresholds that separate bullish and bearish histogram zones. |
| `SignalBar` | Number of closed bars to skip before analysing the histogram. |
| `InvertCorrelation` | Flips the histogram sign to match the MQL `direction=false` behaviour. |
| `AllowBuyEntries`, `AllowSellEntries` | Enable opening long/short positions. |
| `AllowBuyExits`, `AllowSellExits` | Enable automatic closure of existing long/short positions. |
| `TradeMode` | Selects Mode 1, Mode 2, or Mode 3 logic from the original expert. |
| `StopLossPoints`, `TakeProfitPoints` | Optional protective distances in absolute price units for `StartProtection`. |
| `Volume` (built-in) | Base order size used when opening or reversing positions. |

## Differences from the MetaTrader expert

* Money-management inputs (`MM`, `MMMode`) and slippage (`Deviation_`) are not replicated; position sizing relies on the standard `Volume` property and the broker configuration.
* The MQL helper functions from `TradeAlgorithms.mqh` are replaced with direct `BuyMarket`/`SellMarket` calls after cancelling pending orders.
* The `CalculatedBars` performance hint is unnecessary in StockSharp and has been omitted.
* The `direction` flag is represented by `InvertCorrelation`, which simply mirrors the histogram sign.
* Stop-loss and take-profit distances (`StopLoss_`, `TakeProfit_`) are interpreted as absolute price offsets when enabling `StartProtection`; no automatic point-to-price conversion is performed.
* Signal times are handled at the candle close; there is no deferred scheduling to the next bar opening.

These adjustments follow the high-level StockSharp strategy guidelines while preserving the original signal logic.
