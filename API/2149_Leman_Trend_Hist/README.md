# LeMan Trend Hist Strategy

This strategy is a simplified conversion of the original MQL5 expert "LeManTrendHist". It relies on an EMA-based histogram to generate trading signals.

## Idea

The original algorithm calculates a custom histogram derived from price extremes and smoothed ranges. For this sample the histogram is approximated by an exponential moving average of candle ranges.

## Strategy Logic

1. Compute EMA value for each finished candle.
2. Compare the last three EMA values.
3. When the middle value is lower than the oldest and the newest value rises above it, a long position is opened and short positions are closed.
4. When the middle value is higher than the oldest and the newest value falls below it, a short position is opened and long positions are closed.

## Parameters

- **Candle Type** – timeframe of processed candles.
- **EMA Period** – length of the EMA used in the placeholder histogram.
- **Signal Bar** – historical shift for indicator values (kept for compatibility, not used in simplified logic).
- **Buy/Sell Open** – enable long or short entries.
- **Buy/Sell Close** – enable closing of existing positions.

## Notes

The true LeManTrendHist indicator uses complex smoothing algorithms that are not yet implemented. The current implementation acts as a placeholder and should be replaced with the full indicator for production use.
