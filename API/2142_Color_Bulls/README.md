# Color Bulls Strategy

## Overview

This strategy is a port of the MetaTrader expert `Exp_ColorBulls`. It relies on the Color Bulls indicator, which calculates the difference between the candle's high price and a moving average. The resulting value is smoothed by another moving average and displayed as a histogram with different colors for rising and falling values.

The strategy reacts to color changes of this histogram:

- When the indicator switches from rising (green) to falling (magenta), a long position is opened.
- When the indicator switches from falling to rising, a short position is opened.
- Opposite positions are closed automatically before entering new ones.

Only completed candles are processed and market orders are used for entries and exits.

## Parameters

- **Fast MA Length** – period of the moving average applied to high prices.
- **Smooth Length** – period of the moving average used to smooth the bulls value.
- **Candle Type** – timeframe of candles used for calculations.

## Notes

This example demonstrates integration of a custom indicator with the high‑level StockSharp API. Stop‑loss and take‑profit management is not included.
