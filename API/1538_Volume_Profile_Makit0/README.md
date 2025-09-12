# Volume Profile Strategy

This simplified volume profile strategy tracks session high, low and the point of control defined by the price of the candle with the highest volume. The strategy buys when price is above the point of control and sells when it is below. Positions are closed when price returns to the session mid level.

## Parameters
- **Candle Type** – timeframe of input candles.
