# Anchored Momentum Candle Strategy

This strategy converts the MQL5 "AnchoredMomentumCandle" expert into a StockSharp C# sample. It computes anchored momentum for candle open and close prices using exponential and simple moving averages. The indicator draws a synthetic candle whose color reflects momentum direction.

A change to a **blue** candle opens a long position and closes any short. A change to a **pink** candle opens a short position and closes any long.

## Parameters
- **Momentum Period** – length of the simple moving averages.
- **Smooth Period** – length of the exponential moving averages.
- **Candle Type** – timeframe of candles used for calculations.

The strategy subscribes to the specified candles, calculates the indicator, and issues market orders on color transitions.
