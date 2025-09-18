# DeMarker Gaining Position Volume 2 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy reproduces the MetaTrader 5 expert advisor **"DeMarker gaining position volume 2"** using StockSharp's high-level API. It analyses a configurable candle series with the DeMarker oscillator and reacts when the value enters extreme zones. The implementation keeps the original money-management flavour with fixed lot sizing, optional reversal of signals, built-in stop-loss/take-profit handling and an optional trading-session filter.

## Original expert behaviour

* **Platform**: MetaTrader 5.
* **Indicator**: classic DeMarker oscillator (`DEM`), default period 14.
* **Entries**: open longs when DeMarker drops below a lower threshold, open shorts when it rises above an upper threshold.
* **Risk controls**: fixed stop-loss/take-profit expressed in points, optional trailing stop with step, optional time window.
* **Position management**: ensure only one trade per bar and close the opposite side before flipping direction.

The StockSharp conversion follows the same principles. Protective orders are implemented with `StartProtection`, so stop-loss, take-profit and trailing are managed automatically once a position is opened.

## Trading logic

1. Subscribe to the configured candle type (`CandleType`, 5-minute candles by default) and compute the DeMarker value with the chosen period (`DeMarkerPeriod`).
2. When a candle closes, evaluate the oscillator:
   * If `ReverseSignals` is **false** (default):
     * **Long setup** – `DeMarker <= LowerLevel`.
     * **Short setup** – `DeMarker >= UpperLevel`.
   * If `ReverseSignals` is **true**, the long/short rules swap.
3. Only trade within the optional session window defined by `SessionStart`/`SessionEnd` when `UseTimeFilter` is enabled. Overnight sessions are supported.
4. Execute at most one new entry per candle. Before opening a new position the strategy closes any opposite holdings to mirror the MT5 logic.
5. Volumes are fixed by the `TradeVolume` parameter. If the strategy is already partially in the desired direction it tops up to the requested volume.

## Risk management

* `StopLossPoints` and `TakeProfitPoints` (in price steps) map to the expert's point-based stop and take-profit distances.
* Enabling `EnableTrailing` switches the stop distance to `TrailingStopPoints` and activates the built-in trailing engine using `TrailingStepPoints` as the adjustment step.
* `StartProtection` is configured with `useMarketOrders = true` so protective orders will execute immediately, resembling the MT5 trade closure behaviour.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `DeMarkerPeriod` | Averaging period of the DeMarker indicator. |
| `UpperLevel` / `LowerLevel` | Overbought/oversold thresholds triggering shorts/longs. |
| `ReverseSignals` | Swap long and short conditions. |
| `StopLossPoints` | Initial protective stop distance measured in price steps. |
| `TakeProfitPoints` | Take-profit distance measured in price steps. |
| `EnableTrailing` | Enables the trailing stop block. |
| `TrailingStopPoints` | Distance of the trailing stop once trailing is active. |
| `TrailingStepPoints` | Minimum favourable move before the trailing stop is advanced. |
| `UseTimeFilter` | Restricts trading to the `SessionStart`–`SessionEnd` window. |
| `SessionStart` / `SessionEnd` | Inclusive/exclusive session boundaries (supports wrap-around). |
| `TradeVolume` | Quantity to send with each market order. |
| `CandleType` | Candle series to analyse (default 5-minute). |

## Implementation notes

* The MT5 expert included a "trailing activation" threshold. StockSharp's standard trailing protection does not expose the same parameter, therefore trailing activates immediately when `EnableTrailing` is true.
* Error handling for invalid lot sizes, freeze levels and bid/ask refresh logic are handled by StockSharp's infrastructure, so they are omitted from the conversion.
* Logging is performed via the base `Strategy` class (call `LogInfo/LogError` if additional tracing is required).
