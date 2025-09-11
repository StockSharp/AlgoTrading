# Order Block Finder Strategy

This strategy identifies bullish and bearish order blocks based on a specified number of consecutive candles and a minimum percent move. When a bullish order block is detected, the strategy buys; when a bearish block is found, it sells.

## Parameters
- **Relevant Periods** – number of subsequent candles to confirm an order block
- **Min Percent Move** – minimal percent change between the block and the last confirming candle
- **Use Whole Range** – use High/Low range instead of Open-based boundaries
- **Candle Type** – candle type used for calculations
