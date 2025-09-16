# CGOscillator X2 Strategy

## Overview

The **CGOscillator X2 Strategy** is a multi-timeframe trend-following system that uses the Center of Gravity oscillator to trade pullbacks. The strategy evaluates the slope of the oscillator on a higher timeframe to determine the dominant trend and waits for a corrective hook on a lower timeframe before entering a trade in the direction of the trend. Optional stop-loss and take-profit distances expressed in absolute price units can be used to manage risk after an entry is opened.

## Trading Logic

1. **Trend Detection (higher timeframe)**
   - The Center of Gravity (CG) oscillator is calculated on the trend timeframe using the configured `TrendLength`.
   - If the current CG value is above its signal (previous value), the strategy considers the market bullish; if it is below the signal, the market is considered bearish.
2. **Signal Generation (lower timeframe)**
   - A second CG oscillator instance with its own length works on the signal timeframe.
   - The strategy monitors the two most recent finished candles. A bullish hook (current CG >= signal while the previous CG < previous signal) indicates that a pullback finished inside a downtrend. A bearish hook (current CG <= signal while the previous CG > previous signal) highlights a pullback inside an uptrend.
3. **Entries and Exits**
   - Long entries are permitted only when the higher timeframe shows an uptrend and the latest lower timeframe swing indicates a bearish hook (oversold pullback). Shorts follow the mirrored logic for downtrends.
   - Positions can be closed either when the higher timeframe trend flips or when the most recent hook goes against the open position, depending on the boolean parameters.
4. **Risk Controls**
   - Optional absolute stop-loss and take-profit distances are applied after every market entry. When price crosses those levels inside the current candle, the position is closed immediately before new signals are processed.

## Parameters

| Name | Description |
| ---- | ----------- |
| `TrendCandleType` | Candle type (timeframe) used for the higher timeframe CG oscillator. |
| `SignalCandleType` | Candle type used for the lower timeframe signal oscillator. |
| `TrendLength` | Length of the CG oscillator on the trend timeframe. |
| `SignalLength` | Length of the CG oscillator on the signal timeframe. |
| `BuyOpen` | Enables or disables long entries aligned with the higher timeframe trend. |
| `SellOpen` | Enables or disables short entries aligned with the higher timeframe trend. |
| `BuyClose` | Closes long positions when the higher timeframe trend turns bearish. |
| `SellClose` | Closes short positions when the higher timeframe trend turns bullish. |
| `BuyCloseSignal` | Closes long positions when the latest lower timeframe hook is bearish. |
| `SellCloseSignal` | Closes short positions when the latest lower timeframe hook is bullish. |
| `StopLoss` | Absolute price distance for the protective stop (0 disables the stop). |
| `TakeProfit` | Absolute price distance for the profit target (0 disables the target). |

## Indicator Details

The custom **CenterOfGravityOscillatorIndicator** replicates the MT5 CG Oscillator:
- The median price `(high + low) / 2` is used as input.
- A weighted sum of the last `Length` medians forms the CG value.
- The signal line is simply the previous CG value, providing a one-bar lag for hook detection.

## Usage Notes

- Set the `Volume` property of the strategy to control the base order size. Reversals automatically add the absolute value of the current position so that the new position is opened in the desired direction.
- Because the strategy works with finished candles only, it is resilient to intra-bar noise but reacts on the close of each candle.
- The stop-loss and take-profit parameters use absolute price units; adjust them to the instrument's tick size and volatility profile.
- The strategy can be attached to any instrument supported by StockSharp once the appropriate candle types are configured.
