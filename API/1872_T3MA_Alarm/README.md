# T3MA Alarm Trend Strategy

This strategy replicates the idea of the T3MA-ALARM indicator. It applies a double smoothed exponential moving average to detect changes in trend direction.

When the smoothed moving average turns upward it opens a long position. When it turns downward it opens a short position. Optionally an opposite signal can close the current position. Stop-loss and take-profit levels are set as absolute price distances from the entry price.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `MaPeriod` | Length of the exponential moving average. |
| `MaShift` | Number of bars used to detect the direction change. |
| `StopLoss` | Price distance for protective stop-loss. Set `0` to disable. |
| `TakeProfit` | Price distance for take-profit. Set `0` to disable. |
| `ReverseOnSignal` | Close an opposite position when a new signal appears. |
| `CandleType` | Candle type used for calculations. |

## Signals

* **Buy** – the smoothed MA direction changes from down to up.
* **Sell** – the smoothed MA direction changes from up to down.

Positions are closed either by an opposite signal (when enabled) or when stop-loss / take-profit levels are reached.

