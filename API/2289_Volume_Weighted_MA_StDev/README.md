# Volume Weighted MA StdDev Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy applies a Volume Weighted Moving Average (VWMA) with a standard deviation filter. It measures the momentum of VWMA and opens a long position when the upward movement exceeds a configurable deviation threshold. A short position is opened when the downward movement crosses the negative threshold. The approach tries to capture strong directional moves confirmed by volume.

## Parameters
- Candle Type
- VWMA Length
- StdDev Period
- K1
- K2
