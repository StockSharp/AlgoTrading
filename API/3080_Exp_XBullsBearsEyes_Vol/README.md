# Exp XBullsBearsEyes Vol Strategy

## Overview
This strategy is a C# conversion of the MetaTrader expert **Exp_XBullsBearsEyes_Vol**. The original advisor combines Bulls Power and Bears Power readings, multiplies the result by candle volume and colours the histogram according to the resulting momentum. Two independent position slots are maintained for both the long and the short side, allowing the system to scale in when the colour intensity increases. The StockSharp port recreates the multi-stage filter, colour logic and trade management while using high-level API calls for orders and risk control.

The algorithm subscribes to a configurable timeframe, rebuilds the custom XBullsBearsEyes indicator and reacts only to finished candles. Colour transitions determine both the entries and the exits: bullish colours close short trades and can open one or two long slots; bearish colours perform the mirror action. Stop-loss and take-profit distances are translated into `StartProtection` parameters so that platform risk managers can handle protective orders.

## Indicator logic
1. Bulls Power and Bears Power values are rebuilt with an EMA of period `IndicatorPeriod` using the candle high/low against the smoothed close.
2. A four-stage adaptive filter accumulates bullish (`CU`) and bearish (`CD`) pressure with coefficient `Gamma`. The indicator value is `CU / (CU + CD) * 100 - 50`.
3. The filtered value is multiplied by either tick volume or real volume, depending on `VolumeType`.
4. The multiplied series and the raw volume are both smoothed by a moving average chosen through `SmoothingMethod`, `SmoothingLength` and `SmoothingPhase` (Jurik phase is honoured when the underlying class exposes it).
5. Colour levels are derived from `HighLevel1`, `HighLevel2`, `LowLevel1` and `LowLevel2`. Values above the upper bands produce colours `0` or `1`, while values below the lower bands produce colours `3` or `4`. Colour `2` indicates a neutral state.
6. Colour history is stored so that signals can be evaluated on bar `SignalBar` (default: one closed candle back). The colour from the current signal bar is compared to the previous colour to detect transitions.

## Trading rules
- Colours `1` and `0` denote bullish pressure. When the colour changes into one of those values and the previous colour was weaker, slot 1 (`PrimaryVolume`) or slot 2 (`SecondaryVolume`) opens a long position respectively. Both events close any existing short exposure if `AllowShortExit` is enabled.
- Colours `3` and `4` denote bearish pressure. When the colour moves into these values and the previous colour was stronger, slot 1 or slot 2 opens a short position respectively. Both events close any existing long exposure if `AllowLongExit` is enabled.
- Each slot remembers whether it already has an open position and ignores repeated signals until the corresponding direction has been closed.
- `SignalBar` defines how many completed candles are skipped before evaluating the colour (0 = latest finished candle). The code requires at least two historical colours to compare.
- Stop-loss and take-profit expressed in points (`StopLossPoints`, `TakeProfitPoints`) are converted to absolute price distances with `Security.PriceStep` and used to start platform protection with market exits.

## Parameters
| Name | Description |
|------|-------------|
| `PrimaryVolume` | Volume for the first slot (triggered by colour 1 / 3). |
| `SecondaryVolume` | Volume for the second slot (triggered by colour 0 / 4). |
| `StopLossPoints` / `TakeProfitPoints` | Protective distances in price steps. Set to zero to disable. |
| `AllowLongEntry` / `AllowShortEntry` | Enable scaling into the corresponding direction. |
| `AllowLongExit` / `AllowShortExit` | Enable automated exits when the opposite colour appears. |
| `CandleType` | Timeframe subscribed for candles and indicator calculation (default: 8 hours). |
| `IndicatorPeriod` | EMA period used to rebuild Bulls/Bears Power. |
| `Gamma` | Adaptive smoothing factor for the four-stage filter (0.0 – 0.999). |
| `VolumeType` | Select tick volume or real volume for weighting. |
| `HighLevel1`, `HighLevel2`, `LowLevel1`, `LowLevel2` | Level multipliers that define colour thresholds. |
| `SmoothingMethod` | Moving average type used to smooth the indicator and the volume (SMA, EMA, SMMA, LWMA, Jurik, JurX, ParMA→EMA, T3, VIDYA→EMA, AMA). |
| `SmoothingLength` | Length of the smoothing moving average. |
| `SmoothingPhase` | Jurik phase parameter (clamped to [-100, 100]). |
| `SignalBar` | Number of closed candles to step back before evaluating colour transitions. |

## Usage notes
- The strategy operates on a single security returned by `GetWorkingSecurities()` and uses market orders for entries and exits.
- Slot management is netted: additional entries add to the net position, while exits flatten the entire exposure for the affected side.
- If the platform provides only tick volume, selecting `VolumeType = Real` will fall back to the available tick count.
- VIDYA and Parabolic smoothing fall back to exponential moving averages because StockSharp exposes those implementations directly.
- Make sure to configure the instrument price step so that `StopLossPoints` and `TakeProfitPoints` convert into the intended absolute distances.
