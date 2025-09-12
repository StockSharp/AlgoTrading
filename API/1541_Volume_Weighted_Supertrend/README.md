# Volume-Weighted Supertrend Strategy

This strategy calculates a supertrend based on a volume-weighted moving average and an ATR band. A second supertrend is applied to volume to confirm trend strength. A long position opens when volume and price trends align upward, and closes when conditions reverse.

## Parameters
- **ATR Period** – ATR period for price trend.
- **Volume Period** – period for VWAP and volume trend.
- **Factor** – ATR multiplier.
- **Candle Type** – timeframe of processed candles.
