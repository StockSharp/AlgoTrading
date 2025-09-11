# Vortex Cross with MA Confirmation Strategy

This strategy uses the Vortex indicator to detect trend reversals and confirms entries with a smoothed moving average. A long trade is opened when the positive Vortex crosses above the negative one and price is above the smoothing line. A short trade occurs on the opposite cross under the line.

## Parameters
- **Vortex Length** – period for Vortex calculation.
- **SMA Length** – length of the base SMA.
- **Smoothing Length** – length for the smoothing moving average.
- **MA Type** – smoothing method.
- **Candle Type** – timeframe of processed candles.
