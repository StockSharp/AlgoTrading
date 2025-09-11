# Relative Strength Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy computes a weighted relative strength measure from multiple moving averages.
Bollinger bands on the strength signal overbought and oversold zones.
The strategy buys when strength moves above the upper band and sells when it drops below the lower band.

## Details

- **Entry**: strength crosses above upper band for long, below lower band for short.
- **Exit**: opposite band cross.
- **Indicators**: EMA 8, EMA 34, SMA 20, SMA 50, SMA 200, Bollinger Bands.
- **Type**: Momentum.
