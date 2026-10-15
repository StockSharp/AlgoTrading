# Fast2-Kreuzungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie auf Basis des Fast2-Histogramms. Das Histogramm kombiniert den Körper der letzten drei Kerzen mit Quadratwurzel-Gewichten und wendet zwei gewichtete gleitende Durchschnitte an. Eine Long-Position wird eröffnet, wenn der schnelle Durchschnitt den langsamen von oben nach unten kreuzt, und eine Short-Position, wenn er ihn von unten nach oben kreuzt.

## Details

- **Einstiegskriterien**:
  - Long: schnelle WMA kreuzt langsame WMA von oben nach unten
  - Short: schnelle WMA kreuzt langsame WMA von unten nach oben
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Entgegengesetzte Kreuzung
- **Stops**: Keine
- **Standardwerte**:
  - `FastLength` = 3
  - `SlowLength` = 9
  - `CandleType` = TimeSpan.FromHours(8).TimeFrame()
- **Filter**:
  - Kategorie: Kreuzung
  - Richtung: Beide
  - Indikatoren: WeightedMovingAverage
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
