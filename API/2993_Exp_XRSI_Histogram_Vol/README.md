# Exp XRSI Histogram Vol Strategy

## Overview

This strategy is a C# conversion of the original MQL5 expert advisor `Exp_XRSI_Histogram_Vol`. It trades breakouts in the volume-weighted RSI histogram by interpreting the five color states produced by the indicator. The script runs on any timeframe provided through the candle subscription and is built on StockSharp's high-level strategy API.

## Strategy logic

1. Calculate an RSI on the selected timeframe and subtract 50 to center the oscillator.
2. Multiply the centered RSI value by the chosen volume stream (tick or real volume) to emphasize candles with strong activity.
3. Smooth both the weighted RSI and the raw volume using the same moving-average method and length.
4. Build adaptive thresholds by multiplying the smoothed volume by four user-defined multipliers. The resulting histogram is classified into the following color states:
   - **0** – strong bullish impulse (above `HighLevel2`).
   - **1** – moderate bullish impulse (between `HighLevel1` and `HighLevel2`).
   - **2** – neutral zone.
   - **3** – moderate bearish impulse (between `LowLevel2` and `LowLevel1`).
   - **4** – strong bearish impulse (below `LowLevel2`).
5. Entry and exit rules mirror the MQL logic:
   - Enter the first long when the histogram changes into state **1** after being above state **1** (color decreases from bearish/neutral into moderate bullish).
   - Enter the second long when the histogram changes into state **0** after being above state **0**.
   - Enter the first short when the histogram changes into state **3** after being below state **3**.
   - Enter the second short when the histogram changes into state **4** after being below state **4**.
   - Close short positions whenever the histogram is in states **0** or **1**.
   - Close long positions whenever the histogram is in states **3** or **4**.
6. Signal generation can be shifted back by `SignalBar` bars to mimic the original indicator buffer indexing.

Two scaling entries are supported for each direction through the `Mm1` and `Mm2` multipliers. The helper methods flatten an opposite position before opening a new one, replicating the behaviour of the legacy trade management code.

## Money management and protection

- `Mm1` and `Mm2` are volume multipliers applied to the strategy `Volume` property (a default of 1 is used when `Volume` is unset).
- Global stop-loss and take-profit are activated through `StartProtection` when both the price step and the corresponding point values are positive. They are interpreted as a number of price steps.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `CandleType` | Timeframe used for candles and indicator calculations. |
| `RsiPeriod` | RSI length. |
| `VolumeMode` | Choose between tick volume and real volume. Tick mode falls back to one unit when volume data is missing. |
| `HighLevel2`, `HighLevel1`, `LowLevel1`, `LowLevel2` | Multipliers that scale the smoothed volume to build histogram thresholds. |
| `MaMethod`, `MaLength`, `MaPhase` | Smoothing settings. Unsupported methods (Parabolic, T3, Vidya, Ama) fall back to simple moving average. `MaPhase` is kept for completeness but only affects advanced methods such as Jurik. |
| `SignalBar` | How many closed bars back should be evaluated when reading the histogram color. |
| `Mm1`, `Mm2` | Volume multipliers for the first and second positions in each direction. |
| `BuyPosOpen`, `SellPosOpen`, `BuyPosClose`, `SellPosClose` | Enable or disable opening and closing logic for longs/shorts. |
| `StopLossPoints`, `TakeProfitPoints` | Protective offsets expressed in price steps. |

## Default values

- Candle type: 4-hour time frame.
- RSI length: 14.
- Volume mode: tick volume.
- Histogram thresholds: `HighLevel2 = 17`, `HighLevel1 = 5`, `LowLevel1 = -5`, `LowLevel2 = -17`.
- Moving average: SMA with length 12 and phase 15.
- Signal bar offset: 1 bar.
- Money management: `Mm1 = 0.1`, `Mm2 = 0.2`.
- Stops: stop loss 1000 points, take profit 2000 points (applied only when a valid price step is available).

## Notes

- The strategy relies on finished candles and ignores unfinished updates.
- Jurik smoothing is supported via StockSharp's `JurikMovingAverage`. Other legacy methods (ParMA, T3, VIDYA, AMA) revert to SMA due to missing native equivalents.
- The indicator uses the candle's `TotalVolume`. When volume is zero the tick mode uses a fallback weight of one to avoid suppressing signals.
- For visual analysis the RSI itself is plotted alongside candles and trade markers. You can attach additional chart elements if deeper diagnostics are required.
