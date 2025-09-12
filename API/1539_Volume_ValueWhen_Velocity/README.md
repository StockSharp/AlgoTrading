# Volume ValueWhen Velocity Strategy

This strategy seeks long entries when volume expands, the market is oversold based on RSI, volatility measured by ATR is contracting and the distance between recent SMA breakouts exceeds a specified value. When all conditions are satisfied a market buy order is issued.

## Parameters
- **RSI Length** – period for RSI.
- **RSI Oversold** – oversold threshold.
- **ATR Small / ATR Big** – periods for ATR comparison.
- **Distance** – minimum difference between breakout prices.
- **Candle Type** – timeframe of input candles.
