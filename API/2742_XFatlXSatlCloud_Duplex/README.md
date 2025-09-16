# XFatlXSatlCloud Duplex

## Overview
XFatlXSatlCloud Duplex is a dual-direction strategy converted from the original MQL5 expert advisor. It trades crossings of the XFatlXSatlCloud indicator, which blends a fast FATL digital filter with a slower SATL filter and then smooths both with configurable moving averages. Separate configurations can be applied to the long and short sides, including different timeframes, smoothing methods and applied price sources.

## Trading logic
The strategy evaluates finished candles only. Two independent subscriptions run in parallel: one drives the long logic and the other the short logic. Each subscription feeds the XFatlXSatlCloud indicator implemented in C# and produces the following behaviour:

- **Long entry** – triggered when the fast line crosses above the slow line on the bar defined by `LongSignalBar`. If a short position is open it is closed first (only if `ShortAllowClose` is enabled). A market buy order with `LongVolume` contracts is then sent and the entry price is recorded for risk checks.
- **Long exit** – executed when the fast line falls below the slow line at the shifted bar. Optional price based stop loss and take profit checks (`LongStopLoss`, `LongTakeProfit`) may close the position earlier if the candle range violates the defined offsets.
- **Short entry** – triggered when the fast line crosses below the slow line on the bar defined by `ShortSignalBar`. Existing long exposure is flattened first if `LongAllowClose` is enabled. A market sell order with `ShortVolume` contracts is submitted afterwards.
- **Short exit** – executed when the fast line rises above the slow line at the shifted bar. Optional risk controls (`ShortStopLoss`, `ShortTakeProfit`) monitor intrabar extremes.

All indicator values are calculated on finished candles only, ensuring that every decision relies on final data and mirrors the original MQL behaviour.

## Risk management
The strategy keeps track of the last entry price separately for long and short positions. If a stop loss or take profit offset is specified and the current candle breaches the corresponding threshold, the position is closed immediately (subject to the relevant `AllowClose` flag). Offsets are measured in absolute price units of the traded instrument.

## Parameters
| Group | Name | Description |
| --- | --- | --- |
| Trading | `LongVolume` | Order size for long entries (greater than zero). |
| Trading | `ShortVolume` | Order size for short entries (greater than zero). |
| Trading | `LongAllowOpen` | Enable or disable opening new long positions. |
| Trading | `LongAllowClose` | Enable or disable long exits (needed for stops and cross exits). |
| Trading | `ShortAllowOpen` | Enable or disable opening new short positions. |
| Trading | `ShortAllowClose` | Enable or disable short exits. |
| Signals | `LongSignalBar` | Number of completed bars to look back when checking the crossover for longs. |
| Signals | `ShortSignalBar` | Number of completed bars to look back when checking the crossover for shorts. |
| Data | `LongCandleType` | Candle type (timeframe) used for the long indicator subscription. |
| Data | `ShortCandleType` | Candle type used for the short indicator subscription. |
| Indicators | `LongMethod1` | Smoothing method applied to the FATL output on the long side. Supported values: SMA, EMA, SMMA, LWMA, Jurik, ZeroLag, Kaufman. |
| Indicators | `LongLength1` | Length for the fast long smoother. |
| Indicators | `LongPhase1` | Phase parameter forwarded to the fast smoother (kept for compatibility, only Jurik uses it conceptually). |
| Indicators | `LongMethod2` | Smoothing method applied to the SATL output on the long side (same supported set as above). |
| Indicators | `LongLength2` | Length for the slow long smoother. |
| Indicators | `LongPhase2` | Phase parameter for the slow long smoother. |
| Indicators | `LongAppliedPrice` | Applied price used to build the long indicator (close, open, median, typical, weighted, simple, quarter, trend-follow or Demark). |
| Indicators | `ShortMethod1` | Smoothing method for the fast short line. |
| Indicators | `ShortLength1` | Length for the fast short smoother. |
| Indicators | `ShortPhase1` | Phase parameter for the fast short smoother. |
| Indicators | `ShortMethod2` | Smoothing method for the slow short line. |
| Indicators | `ShortLength2` | Length for the slow short smoother. |
| Indicators | `ShortPhase2` | Phase parameter for the slow short smoother. |
| Indicators | `ShortAppliedPrice` | Applied price used to build the short indicator. |
| Risk | `LongStopLoss` | Absolute price distance for the long stop loss (0 disables the check). |
| Risk | `LongTakeProfit` | Absolute price distance for the long take profit (0 disables the check). |
| Risk | `ShortStopLoss` | Absolute price distance for the short stop loss (0 disables the check). |
| Risk | `ShortTakeProfit` | Absolute price distance for the short take profit (0 disables the check). |

## Implementation notes
- The XFatlXSatlCloud indicator is implemented as a high-level StockSharp indicator. The fast and slow components are produced by applying the original FATL/SATL finite impulse response coefficients followed by user-selected smoothing indicators.
- Only commonly available StockSharp moving averages are exposed (`Sma`, `Ema`, `Smma`, `Lwma`, `Jurik`, `ZeroLag`, `Kaufman`). Other MQL smoothing families (such as Parabolic or T3) are not included.
- `LongSignalBar` and `ShortSignalBar` mimic the original `SignalBar` parameter. A value of 1 means “use the previous completed bar” when detecting the crossover.
- Stop-loss and take-profit offsets expect absolute price distances. They are applied using the candle high/low relative to the recorded entry price and do not rely on broker-specific point values.
