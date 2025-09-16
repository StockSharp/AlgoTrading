# XDeMarker Histogram Vol Direct Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy replicates the MetaTrader 5 expert **Exp_XDeMarker_Histogram_Vol_Direct** using StockSharp's high-level API. It
multiplies the DeMarker oscillator by the chosen volume stream, smooths both the oscillator and the volume with the same moving
average, and compares the result with configurable upper/lower levels. Trading decisions are made when the smoothed histogram
changes direction between consecutive bars.

## Indicator logic

1. Calculate the classic DeMarker oscillator on the selected timeframe.
2. Scale the oscillator by tick count or real volume for each finished candle.
3. Smooth both the histogram and the volume with the selected moving-average type.
4. Multiply the smoothed volume by the configured level multipliers to obtain four dynamic bands.
5. Detect the histogram direction (rising or falling). When the direction flips, the strategy opens a new position in the
   corresponding direction while also closing any opposite trade.

The smoothing method supports simple, exponential, smoothed (RMA/SMMA) and weighted moving averages. Exotic filters from the
original library (JJMA, JurX, ParMA, T3, VIDYA, AMA) are not available in this port.

## Trading rules

- **Long entry** – enabled when `Allow Long Entry = true`. If the previous bar had an "up" direction and the last bar switched to
  "down", the strategy targets a long position of `Volume` lots.
- **Short entry** – enabled when `Allow Short Entry = true`. Triggered when the previous bar was "down" and the latest bar turns
  "up".
- **Long exit** – enabled when `Allow Long Exit = true`. If the previous bar direction is "down", the position is flattened unless
  a new short entry is fired in the same bar.
- **Short exit** – enabled when `Allow Short Exit = true`. Activated when the previous bar direction is "up".

Signals are evaluated once per finished candle. The StockSharp implementation keeps the original one-bar delay; the `Signal Bar`
parameter is present for reference but values different from `1` are ignored with a warning.

## Parameters

| Parameter | Description |
|-----------|-------------|
| Candle Type | Timeframe used to build candles for the indicator. |
| DeMarker Period | Lookback for the base DeMarker oscillator. |
| Volume Source | Choose between tick count and real traded volume. |
| High Level 2 / High Level 1 | Multipliers applied to the smoothed volume to form upper bands. |
| Low Level 1 / Low Level 2 | Multipliers for lower bands. |
| Smoothing Method | Moving-average type applied to both the histogram and the volume. |
| Smoothing Length | Length of the smoothing window. |
| Smoothing Phase | Compatibility placeholder (not used but kept for parity). |
| Signal Bar | Historical offset, fixed to 1 just like in the expert. |
| Allow Long/Short Entry | Enable opening positions in the respective direction. |
| Allow Long/Short Exit | Enable automatic closure of existing trades. |

## Implementation notes

- The `XDeMarkerHistogramVolDirectIndicator` class reproduces the MT5 indicator buffers and exposes the smoothed histogram,
  bands and direction flags through a complex indicator value.
- When a new target exposure is required, the strategy sends a single market order that moves the current position to the desired
  level (`Volume`, `-Volume` or flat). This mimics the sequential close/open calls in the original MQL5 code without duplicating
  orders.
- Chart rendering automatically plots the candles, the custom indicator and the executed trades when a chart area is available.
