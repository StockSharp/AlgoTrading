# Mean Deviation Index Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

The strategy uses the Mean Deviation Index (MDX) to trade deviations from an ATR-filtered EMA.
A long position is opened when the MDX rises above the specified level,
and a short position is opened when it falls below the negative level.

## Details

- **Entry**:
  - Long when MDX > Level
  - Short when MDX < -Level
- **Exit**: Opposite signal.
- **Indicators**: EMA and ATR.
