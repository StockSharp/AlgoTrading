# Mean Deviation Index-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie verwendet den Mean Deviation Index (MDX), um Abweichungen von einer ATR-gefilterten EMA zu handeln.
Eine Long-Position wird eröffnet, wenn der MDX über das angegebene Niveau steigt,
und eine Short-Position, wenn er unter das negative Niveau fällt.

## Details

- **Einstieg**:
  - Long wenn MDX > Level
  - Short wenn MDX < -Level
- **Ausstieg**: entgegengesetztes Signal.
- **Indikatoren**: EMA und ATR.
