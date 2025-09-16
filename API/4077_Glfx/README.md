# GLFX Strategy

MetaTrader 4 expert advisor **GLFX** rewritten for StockSharp's high-level API. The port preserves the original idea of combining higher-timeframe confirmations with strict money-management gates while removing the massive collection of rarely-used filters that depended on external indicators.

## Trading logic

1. The strategy works on a primary timeframe (default **M15**) and optionally builds a confirmation timeframe by walking up the classic MetaTrader ladder (`M15 → M30 → H1 → H4 → D1 → W1 → MN`).
2. A higher-timeframe **RSI** (default period 57) tracks whether momentum is rising or falling. A buy confirmation appears when RSI ticks up but remains below the configured overbought ceiling. A sell confirmation requires RSI to tick down while staying above the oversold floor.
3. A higher-timeframe **simple moving average** (default period 60) detects whether price is moving away from the mean. A bullish confirmation needs the MA to rise while remaining above the current close (price pulling back into an uptrend). A bearish confirmation mirrors this logic.
4. Each enabled filter contributes `+1` for bullish or `-1` for bearish sentiment. The total must reach the number of active filters to count as a valid signal. Counters remember how many consecutive full-strength signals appeared (`SignalsRepeat`). If the combined strength drops below the threshold and `SignalsReset` is enabled, the counters reset.
5. When the strategy is flat and the long/short entry switches allow it, the next completed counter triggers a market order with the configured `Volume`. Static stop-loss and take-profit levels are converted from pips into price offsets using the instrument's tick size.
6. If a position is already open, strong opposite signals can close it early (`AllowLongExit` / `AllowShortExit`). Otherwise, exits rely on the stop or target managed by `StartProtection()`.

The port does **not** reproduce the original EA's optional Quantum, Twitter sentiment, open-bar correlation, set testing, or advanced money-management ladders. Those modules required additional custom indicators or broker state that do not exist in StockSharp.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `CandleType` | M15 | Working timeframe for price evaluation. |
| `HigherTimeFrameShift` | 1 | Number of MT4 steps used to build the confirmation timeframe. `0` keeps the current timeframe. |
| `UseRsiSignal` | true | Enable higher-timeframe RSI confirmation. |
| `RsiPeriod` | 57 | Period of the confirmation RSI. |
| `RsiUpperThreshold` | 65 | Disable new longs once RSI exceeds this value. |
| `RsiLowerThreshold` | 25 | Disable new shorts once RSI drops below this value. |
| `UseMaSignal` | true | Enable higher-timeframe moving average confirmation. |
| `MaPeriod` | 60 | Period of the confirmation moving average. |
| `SignalsRepeat` | 1 | Number of consecutive full-strength signals required before opening a trade. |
| `SignalsReset` | true | Reset counters when the combined signal loses momentum. |
| `TakeProfitPips` | 308 | Take-profit distance expressed in pips. Set to `0` to disable. |
| `StopLossPips` | 290 | Stop-loss distance expressed in pips. Set to `0` to disable. |
| `Volume` | 0.1 | Order size used for new positions (lots). |
| `AllowLongEntry` / `AllowShortEntry` | true | Permission switches for opening long or short trades. |
| `AllowLongExit` / `AllowShortExit` | true | Allow automatic closing of existing exposure on opposite signals. |

## Usage notes

- Choose instruments with a reliable tick size so the pip conversion remains accurate. Forex pairs with three or five decimals are automatically mapped to MetaTrader "points" by multiplying the price step by ten.
- Set `HigherTimeFrameShift` to `0` if you want to run everything on the same timeframe. In this case the indicators are fed by the primary candle stream to avoid duplicate subscriptions.
- If you need the legacy behaviour of keeping trades open regardless of opposite signals, disable the corresponding `Allow*Exit` flag.
- Money-management scaling (`MMC_*` settings), trailing modules, and exotic exit filters from the original script were intentionally omitted. Implement them on top of this clean core if necessary.

## Differences from the original EA

| Feature group | MetaTrader EA | StockSharp port |
|---------------|---------------|-----------------|
| Confirmation filters | RSI, MA, optional Quantum, TSI, multi-currency correlation | RSI and MA only (core behaviour) |
| Entry gating | Signal repetition plus temporal filters | Signal repetition plus optional reset |
| Risk control | Static TP/SL with large trailing module library | Static TP/SL via `StartProtection()` |
| Money management | Incremental lot scaling and loss ladders | Fixed volume parameter |
| External dependencies | Custom indicators (`Quantum`, `TSI`, file-based set loading) | None |

The result is a compact, maintainable strategy that keeps the recognisable GLFX behaviour—waiting for trend confirmation on a slower chart and entering only after repeated agreement—while being easy to extend using the StockSharp framework.
