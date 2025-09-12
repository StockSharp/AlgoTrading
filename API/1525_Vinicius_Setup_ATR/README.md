# Vinicius Setup ATR Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy combines SuperTrend direction, RSI, and volume to identify strong momentum candles. A long signal occurs when price is above the SuperTrend, the candle body exceeds an ATR-based threshold, volume is higher than average, and RSI remains below 70. A short signal triggers under the opposite conditions with RSI above 30.

## Details
- **Entry**: Price in SuperTrend direction with strong candle and high volume.
- **Exit**: Opposite signal.
- **Indicators**: SuperTrend, RSI, ATR, SMA(Volume).
