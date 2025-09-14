# Kalman Filter Candles Strategy

This strategy applies the Kalman Filter to the open and close prices of each candle. The resulting smoothed candles are classified as bullish or bearish depending on whether the smoothed close is above or below the smoothed open. Positions are opened when the candle color changes:

- **Bullish (pink)** &rarr; opens a long position and closes any short position.
- **Bearish (blue)** &rarr; opens a short position and closes any long position.

## Parameters

- `Process Noise` &ndash; smoothing factor for the Kalman Filter.
- `Candle Type` &ndash; timeframe of candles used in the strategy.

## How It Works

1. For every finished candle, the open and close prices are individually smoothed using separate Kalman Filters.
2. A bullish signal is generated when the smoothed close exceeds the smoothed open. A bearish signal occurs when the smoothed close is below the smoothed open.
3. The strategy enters a long position on a bullish signal and a short position on a bearish signal. Opposite positions are closed automatically.

The strategy is intended as an example of combining multiple Kalman Filters to form a simple trend‑following system.
