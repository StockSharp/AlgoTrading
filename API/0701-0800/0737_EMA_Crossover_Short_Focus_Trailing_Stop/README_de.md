# EMA-Crossover-Short-Fokus-Trailing-Stop-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie geht long, wenn die 13-EMA über der 33-EMA liegt und keine Short-Position existiert. Sie geht short, wenn die 13-EMA unter der 33-EMA liegt und keine Long-Position offen ist. Positionen werden geschlossen, wenn die 13-EMA die entgegengesetzte EMA kreuzt, und ein Trailing Stop folgt den jüngsten Extremen.

## Details
- **Einstiegskriterien:**
  - **Long:** 13 EMA ≥ 33 EMA und Position ≤ 0.
  - **Short:** 13 EMA ≤ 33 EMA und Position ≥ 0.
- **Long/Short:** beide.
- **Ausstiegskriterien:** Long schließt wenn 13 EMA < 33 EMA; Short schließt wenn 13 EMA > 25 EMA.
- **Stops:** Trailing Stop mit Abstand `TrailDistance` und Versatz `TrailOffset`.
- **Standardwerte:** short EMA = 13, mid EMA = 25, long EMA = 33, trail distance = 10, trail offset = 2.
