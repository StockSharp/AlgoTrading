# CoeffofLine True Strategy

This strategy ports the MQL5 expert `Exp_CoeffofLine_true.mq5` to the StockSharp framework. It tracks the **Linear Regression Slope** of median prices and reacts to zero crossings.

A long position is opened when the slope becomes positive after being negative. A short position is opened when the slope turns negative after being positive. Existing positions are closed on opposite signals. Only completed candles are processed.

## Parameters

- **Candle Type** – timeframe for the candle series.
- **Slope Period** – length of the linear regression used to compute the slope.
- **Signal Bar** – historical bar index used for signal evaluation.
- **Buy Open / Sell Open** – permissions to open long or short positions.
- **Buy Close / Sell Close** – permissions to exit long or short positions.

The strategy subscribes to candles, binds the indicator via the high-level API and operates without manual indicator value requests.
