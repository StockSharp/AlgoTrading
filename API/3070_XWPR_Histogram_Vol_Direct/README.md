# Exp XWPR Histogram Vol Direct Strategy

## Overview

This strategy is a StockSharp port of the MetaTrader expert advisor **Exp_XWPR_Histogram_Vol_Direct**. It reproduces the original
approach of weighting Williams %R values by volume, smoothing the result, and opening trades when the histogram slope changes
color. Orders are triggered on fully-formed candles and use optional protective stop-loss and take-profit measured in price steps.

## Core Logic

1. Calculate Williams %R on the selected timeframe.
2. Shift the oscillator by +50, multiply it by the chosen volume source (tick or real), and smooth the stream with a configurable
   moving average.
3. Smooth the raw volume with the same moving average to rebuild the indicator bands (HighLevel2, HighLevel1, LowLevel1, LowLevel2).
4. Track the color of the histogram slope: blue (`0`) when the smoothed value rises, magenta (`1`) when it falls. The strategy
   keeps a short history buffer to compare the last two completed colors respecting the `SignalShift` parameter.
5. Execute actions when the previous color changes:
   - Color transition `0 → 1`: close shorts (if enabled) and optionally open a new long position.
   - Color transition `1 → 0`: close longs (if enabled) and optionally open a new short position.

The zone classification (Neutral/Bullish/Bearish/Extreme) is logged for context but does not block trades, matching the behavior of
the original advisor which reads the color buffer only.

## Parameters

| Parameter | Description |
| --- | --- |
| `WilliamsPeriod` | Lookback length for Williams %R. |
| `HighLevel2`, `HighLevel1`, `LowLevel1`, `LowLevel2` | Multipliers applied to the smoothed volume to rebuild the indicator bands. |
| `SmoothingType` | Moving average family used for both the weighted value and the volume streams (SMA, EMA, SMMA, WMA, Hull, VWMA, DEMA, TEMA). |
| `SmoothingLength` | Length of the smoothing moving average. |
| `SignalShift` | How many bars back to read the color buffer (1 reproduces the MetaTrader default). |
| `EnableLongEntries` / `EnableShortEntries` | Allow or block opening long/short positions. |
| `EnableLongExits` / `EnableShortExits` | Allow or block closing long/short positions. |
| `VolumeSource` | Choose between tick count or real volume for weighting. |
| `StopLossPoints` / `TakeProfitPoints` | Optional protective targets expressed in price steps. |
| `CandleType` | Candle type and timeframe used for analysis and trading. |

Use the base `Volume` property of the strategy to define the entry size. Position reversal is handled by sending the absolute
position quantity plus the configured lot size, similar to the MQL expert advisor.

## Usage Notes

- The smoothing phase (`MA_Phase` in MetaTrader) is not supported because StockSharp moving averages do not expose that parameter.
- Ensure sufficient history is loaded for the chosen timeframe so that the moving averages are fully formed before trading starts.
- The strategy works on any instrument supported by StockSharp; set `CandleType` to the desired resolution (for example 4-hour
  time frame to match the original defaults).
- Tick-volume weighting requires data sources that provide tick counts inside candle messages. Otherwise, switch to real volume.

## Logging and Visualization

The strategy draws candles and the Williams %R indicator on the default chart area. Trade actions log the detected zone and the
smoothed histogram value to aid debugging and comparison with the MetaTrader reference implementation.
