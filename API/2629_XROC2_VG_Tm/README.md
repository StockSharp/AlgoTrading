# XROC2 VG Time Filter Strategy

This strategy replicates the MetaTrader expert advisor **Exp_XROC2_VG_Tm** using the StockSharp high-level API. It builds two smoothed rate-of-change (ROC) curves and opens contrarian trades when the faster curve crosses the slower one. A trading session filter and optional protective targets reproduce the original money-management settings.

## Trading logic

- Two ROC values are calculated from the closing price using independent lookbacks.
- Each ROC stream is smoothed with a configurable moving-average method.
- Signals are evaluated on a shifted bar index, matching the original `SignalBar` behavior.
- When the fast line was above the slow line on the prior bar but falls below it on the signal bar, the strategy closes any short position and may open a long position.
- When the fast line was below the slow line on the prior bar but rises above it on the signal bar, the strategy closes any long position and may open a short position.
- An optional trading window can flat all positions outside the allowed session before any new trades are placed.

The order side switches only after the previous position is fully closed, mimicking the MetaTrader trade algorithms.

## Indicators

- **Fast ROC** – Momentum, percent or ratio of price change over `RocPeriod1` bars, smoothed with `SmoothMethod1` and length `SmoothLength1`.
- **Slow ROC** – Same calculation over `RocPeriod2` bars, smoothed with `SmoothMethod2` and length `SmoothLength2`.
- Supported smoothing methods: Simple, Exponential, Smoothed (RMA) and Weighted moving averages. The original JJMA/VIDYA/AMA options are approximated by exponential smoothing.

## Risk management

- `StopLoss` and `TakeProfit` specify optional fixed-distance exits in absolute price units. When either threshold is hit the position is closed immediately.
- `OrderVolume` defines the size of all new positions.
- Indicator-based exits may also flatten positions even if the protective targets are disabled.

## Session filter

- `UseTimeFilter` toggles the time-of-day window.
- `StartTime` / `EndTime` specify the session bounds. When the interval wraps around midnight the window is treated as two segments, exactly as in the MQL version.
- If a position is still open when the window closes it is liquidated at market before the strategy evaluates fresh entries.

## Parameters

| Parameter | Description |
| --- | --- |
| `CandleType` | Candle data type used for calculations (default: 4-hour candles). |
| `RocPeriod1`, `RocPeriod2` | Lookback lengths for the fast and slow ROC streams. |
| `SmoothLength1`, `SmoothLength2` | Smoothing lengths for each stream. |
| `SmoothMethod1`, `SmoothMethod2` | Moving-average types applied to the ROC outputs. |
| `RocType` | ROC calculation formula: momentum, percent change or ratio. |
| `SignalShift` | Number of completed bars back used to read signal values. |
| `AllowBuyOpen`, `AllowSellOpen` | Enable or disable opening long/short positions. |
| `AllowBuyClose`, `AllowSellClose` | Enable or disable indicator-based exits for long/short positions. |
| `UseTimeFilter` | Activates the trading session window. |
| `StartTime`, `EndTime` | Session start and end times. |
| `OrderVolume` | Volume for each new trade. |
| `StopLoss`, `TakeProfit` | Optional absolute distances for protective exits. |

## Implementation notes

- The strategy keeps short histories of prices and smoothed values instead of using indicator buffers, which reproduces the original `SignalBar` offset without relying on `GetValue`.
- JJMA, VIDYA and AMA smoothing from the MQL indicator are mapped to exponential smoothing to stay within the StockSharp standard indicator set.
- All comments in the code are in English and the namespace follows the repository guidelines.
