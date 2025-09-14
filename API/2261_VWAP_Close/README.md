# VWAP Close Strategy

## Overview
This strategy calculates a Volume Weighted Moving Average (VWMA) of closing prices. When the VWMA changes direction, it acts as a signal for potential entries or exits:

- If the VWMA was falling and turns upward (forms a valley), the strategy closes any short position and may open a long position.
- If the VWMA was rising and turns downward (forms a peak), the strategy closes any long position and may open a short position.

## Parameters
- **Period** – number of candles used for VWMA calculation.
- **Candle Type** – timeframe of processed candles.
- **Buy Open** – enable opening long positions.
- **Sell Open** – enable opening short positions.
- **Buy Close** – allow closing long positions when the VWMA turns down.
- **Sell Close** – allow closing short positions when the VWMA turns up.

## Notes
The strategy uses `VolumeWeightedMovingAverage` indicator from StockSharp and processes only finished candles. Trade volume is taken from the strategy's `Volume` property; when opening a new position, any opposite position is closed automatically.
