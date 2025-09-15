# Bear Bulls Power Strategy

This strategy is a conversion of the MetaTrader 5 expert "Exp_Bear_Bulls_Power". It uses a smoothed Bear/Bulls Power indicator to detect trend reversals.

## How it works

1. Calculate the median price of each candle: `(High + Low) / 2`.
2. Smooth the median price with a moving average of length `FirstLength`.
3. Compute the difference between the median price and its moving average.
4. Apply a second smoothing with a moving average of length `SecondLength`.
5. Determine trend direction by comparing the current smoothed value with the previous one.
6. Generate signals when the direction changes:
   - Upward turn above zero opens a long position.
   - Downward turn below zero opens a short position.

## Parameters

- **Candle Type** – timeframe of processed candles.
- **First Length** – period for price smoothing.
- **Second Length** – period for signal smoothing.

The strategy uses market orders and works on completed candles only.
