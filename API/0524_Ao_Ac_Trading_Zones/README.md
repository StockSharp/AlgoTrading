# AO AC Trading Zones Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy reproduces the "AO/AC Trading Zones" concept. It combines the Awesome Oscillator (AO), Acceleration/Deceleration (AC), and Bill Williams fractals to build a pyramid of long positions when momentum accelerates above the Alligator teeth line.

## Details

- **Entry**: At least two consecutive bars with `close > teeth`, `AO > AO[1]`, `AC > AC[1]`, and `close > EMA`.
- **Pyramiding**: Adds up to five long positions while conditions remain valid.
- **Exit**: Fractal trend reversal or price dropping below the stop level.
- **Indicators**: SMMA (teeth), AO, AC, EMA.
- **Stop**: Low of the fifth green bar.
