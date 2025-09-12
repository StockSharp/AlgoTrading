# Relative Candle Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy compares the price change of the current instrument to an index.
It calculates relative open, high, low and close values and a moving average of the relative close.

## Parameters

- **IndexSymbol** – index used for comparison, default `IXIC`.
- **AverageCloseLength** – period for the moving average of the relative close, default `10`.
- **AverageZoomFactor** – scale factor for the average relative close, default `5`.
- **CandleType** – candle type to process.
