# Exp XWPR Histogram Vol Strategy

## Overview
This strategy is a C# conversion of the MetaTrader expert **Exp_XWPR_Histogram_Vol**. It trades on the colour changes of the
XWPR Histogram Vol custom indicator, which multiplies the Williams %R oscillator by the candle volume and smooths the result. The
port keeps the original two-slot money management scheme (primary and secondary volume) and reproduces the same colour-driven
entry and exit rules while using the StockSharp high-level API.

The algorithm processes finished candles only. On each new bar it inspects the histogram colour a configurable number of bars
ahead in the past and reacts when the colour transitions cross the bullish or bearish thresholds defined by the indicator.

## Indicator logic
1. Williams %R (`WprPeriod`) is shifted by +50 and multiplied by the selected candle volume (`VolumeMode`).
2. Both the weighted Williams %R and the raw volume pass through identical smoothing filters (`SmoothingMethod`,
   `SmoothingLength`, `SmoothingPhase`).
3. Four dynamic levels are derived from the smoothed volume: `HighLevel2`, `HighLevel1`, `LowLevel1` and `LowLevel2`.
4. Histogram colours correspond to the zones defined by those levels:
   - **0** – histogram above `HighLevel2` (strong bullish).
   - **1** – histogram between `HighLevel1` and `HighLevel2` (moderate bullish).
   - **2** – histogram between `LowLevel1` and `HighLevel1` (neutral).
   - **3** – histogram between `LowLevel2` and `LowLevel1` (moderate bearish).
   - **4** – histogram below `LowLevel2` (strong bearish).

## Signal rules
The strategy reads two historical colours per evaluation: bar `SignalBar + 1` (older) and bar `SignalBar` (more recent).

- **Open primary long (volume = `PrimaryVolume`)** when the older bar colour is `1` and the newer bar colour moves to `2`, `3` or
  `4`. The move simultaneously requests the closing of any short positions.
- **Open secondary long (volume = `SecondaryVolume`)** when the older bar colour is `0` and the newer bar colour becomes
  anything other than `0`. The same signal also closes shorts.
- **Open primary short (volume = `PrimaryVolume`)** when the older bar colour is `3` and the newer bar colour rises to `0`, `1`
  or `2`, while also closing longs.
- **Open secondary short (volume = `SecondaryVolume`)** when the older bar colour is `4` and the newer bar colour becomes
  `0`, `1`, `2` or `3`, again forcing long exits.
- **Close longs** whenever the older colour is `3` or `4` (bearish zone).
- **Close shorts** whenever the older colour is `0` or `1` (bullish zone).

Two independent position slots are maintained for each direction. A signal only triggers an order if the corresponding slot is
currently inactive and the relevant entry flag (`AllowLongEntry`, `AllowShortEntry`) permits it.

## Risk management
- `StopLossSteps` and `TakeProfitSteps` are translated to StockSharp protective orders via `StartProtection`. The values are
  expressed in instrument price steps.
- `DeviationSteps` is preserved for compatibility with the MQL input list. StockSharp market orders do not use it.

## Parameters
| Name | Description |
|------|-------------|
| `CandleType` | Timeframe used to build the candles supplied to the indicator. |
| `PrimaryVolume`, `SecondaryVolume` | Volumes applied by the level-one and level-two slots. |
| `AllowLongEntry`, `AllowShortEntry` | Enable opening new long or short positions. |
| `AllowLongExit`, `AllowShortExit` | Enable closing long or short exposure when exit signals appear. |
| `StopLossSteps`, `TakeProfitSteps` | Optional protective distances in price steps (0 disables the respective protection). |
| `DeviationSteps` | Reserved for compatibility; has no effect on StockSharp orders. |
| `SignalBar` | Number of closed candles to shift the signal evaluation (0 = latest finished candle). |
| `WprPeriod` | Lookback period for the Williams %R calculation. |
| `VolumeMode` | Selects between tick count (`Tick`) or real volume (`Real`) in the histogram. |
| `HighLevel2`, `HighLevel1` | Multipliers defining the upper bullish thresholds. |
| `LowLevel1`, `LowLevel2` | Multipliers defining the lower bearish thresholds. |
| `SmoothingMethod` | Moving average type used for both the histogram and the baseline volume. |
| `SmoothingLength` | Length of the smoothing filters. |
| `SmoothingPhase` | Phase forwarded to Jurik-based smoothers (ignored by other methods). |

## Usage notes
- The strategy trades a single security returned by `GetWorkingSecurities()` and uses market orders for all actions.
- Signals are evaluated once per finished candle. The additional history buffer prevents duplicate orders on the same bar.
- The two entry slots act independently. Disable a slot by setting the corresponding volume to `0` or disabling the
  `Allow*Entry` flag.
- The conversion does not replicate MetaTrader magic numbers or margin modes. Portfolio sizing is entirely controlled by the
  `PrimaryVolume` and `SecondaryVolume` parameters.
