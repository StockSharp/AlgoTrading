# Exp RJTX Matches Smoothed Duplex

## Overview
The strategy recreates the behaviour of the MetaTrader 5 expert advisor `Exp_RJTX_Matches_Smoothed_Duplex.mq5`. Two independent RJTX signal blocks analyse smoothed open and close prices on their respective time-frames. Each block classifies every finished candle as bullish or bearish depending on whether the smoothed close rises above the smoothed open from `Period` bars ago. Bullish "matches" trigger entries for the long module, while bearish matches manage the short module.

## Signal generation
1. **Smoothing** – both blocks feed candle opens and closes into the selected smoothing algorithm. The same method is applied to open and close streams but separate instances are used to keep the internal buffers independent.
2. **Comparison** – once enough history is available, the current smoothed close is compared with the smoothed open recorded `Period` bars earlier.
3. **Match detection** – if the close is greater, the candle receives a bullish match; otherwise it becomes bearish. Signals are evaluated after shifting by `SignalBar` closed candles, just like the MT5 buffer access.

## Position management
- The **long block** opens a long position (covering any existing short if allowed) when a bullish match reaches the evaluation window. A bearish match closes the long position if long exits are enabled.
- The **short block** mirrors this logic: a bearish match opens a short trade (closing long exposure if permitted) and a bullish match covers the short.
- StockSharp strategies are netted. Therefore, opposite modules close the current position before opening a new one, instead of maintaining two independent hedged positions like the MT5 version. Disable the corresponding `Allow ... Close` parameter to forbid automatic covering.

## Risk management
Stops and profit targets are expressed in price steps (`PriceStep × points`). For every finished candle the strategy checks whether the bar range touches the active stop-loss or take-profit level and closes the corresponding position immediately. This emulates the behaviour of MT5 protective orders without relying on broker-managed orders.

## Parameters
| Section | Parameter | Default | Description |
| --- | --- | --- | --- |
| Long | `LongCandleType` | H4 | Time-frame used by the long RJTX block. |
| Long | `LongVolume` | 0.1 | Volume opened when a long signal is executed. |
| Long | `LongAllowOpen` | `true` | Enable opening long positions. |
| Long | `LongAllowClose` | `true` | Enable closing long positions on bearish matches. |
| Long | `LongStopLossPoints` | 1000 | Stop-loss distance for long trades in price steps (0 disables the check). |
| Long | `LongTakeProfitPoints` | 2000 | Take-profit distance for long trades in price steps (0 disables the check). |
| Long | `LongSignalBar` | 1 | Shift applied when reading RJTX buffers (`0` = current closed candle). |
| Long | `LongPeriod` | 10 | Number of bars between the current smoothed close and the historical smoothed open. |
| Long | `LongMethod` | `Sma` | Smoothing algorithm used for the long block (`Sma`, `Ema`, `Smma`, `Lwma`, `Jjma`, `Jurx`, `Parma`, `T3`, `Vidya`, `Ama`). |
| Long | `LongLength` | 12 | Length of the smoothing filter applied to open/close series. |
| Long | `LongPhase` | 15 | Phase parameter for Jurik-style filters (kept for compatibility). |
| Short | `ShortCandleType` | H4 | Time-frame used by the short RJTX block. |
| Short | `ShortVolume` | 0.1 | Volume opened when a short signal is executed. |
| Short | `ShortAllowOpen` | `true` | Enable opening short positions. |
| Short | `ShortAllowClose` | `true` | Enable closing short positions on bullish matches. |
| Short | `ShortStopLossPoints` | 1000 | Stop-loss distance for short trades in price steps (0 disables the check). |
| Short | `ShortTakeProfitPoints` | 2000 | Take-profit distance for short trades in price steps (0 disables the check). |
| Short | `ShortSignalBar` | 1 | Shift applied when reading RJTX buffers for the short block. |
| Short | `ShortPeriod` | 10 | Number of bars between the current smoothed close and the historical smoothed open. |
| Short | `ShortMethod` | `Sma` | Smoothing algorithm used for the short block. |
| Short | `ShortLength` | 12 | Length of the smoothing filter applied to short signals. |
| Short | `ShortPhase` | 15 | Phase parameter for Jurik-style filters in the short block. |

## Notes
- `Jjma` maps to the Jurik Moving Average. `Jurx`, `Parma`, and `Vidya` are approximated with Zero-Lag EMA, Arnaud Legoux MA, and EMA respectively because StockSharp does not expose identical filters from the SmoothAlgorithms library.
- The stop-loss / take-profit logic is evaluated on candle extremes. Intrabar spikes shorter than the candle's high/low will not trigger exits.
- Signals are processed on finished candles only; intrabar matches are ignored in line with the MT5 `IsNewBar` behaviour.
