# Vortex Cross with MA Confirmation Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy uses the Vortex indicator to detect trend reversals and confirms entries with a smoothed moving average. A long trade is opened when the positive Vortex crosses above the negative one and price is above the smoothing line. A short trade occurs on the opposite cross under the line.

## Parameters
- **Vortex Length** – period for Vortex calculation.
- **SMA Length** – length of the base SMA.
- **Smoothing Length** – length for the smoothing moving average.
- **MA Type** – smoothing method.
- **Candle Type** – timeframe of processed candles.
