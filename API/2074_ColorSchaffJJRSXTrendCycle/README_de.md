# ColorSchaff JJRSX Trend-Zyklus-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet den Schaff Trend Cycle Oszillator auf Basis von JJRSX-Durchschnitten. Sie eröffnet Long- oder Short-Positionen, wenn der Oszillator benutzerdefinierte Levels kreuzt.

## Details

- **Einstiegskriterien**:
  - Kaufen, wenn der Schaff Trend Cycle `HighLevel` von unten kreuzt. Eine bestehende Short-Position wird zuerst geschlossen.
  - Verkaufen, wenn der Schaff Trend Cycle `LowLevel` von oben kreuzt. Eine bestehende Long-Position wird zuerst geschlossen.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Positionen werden bei einem entgegengesetzten Einstiegssignal geschlossen.
- **Stops**: Keine.
- **Standardwerte**:
  - `Fast` = 23
  - `Slow` = 50
  - `Cycle` = 10
  - `HighLevel` = 60
  - `LowLevel` = -60
