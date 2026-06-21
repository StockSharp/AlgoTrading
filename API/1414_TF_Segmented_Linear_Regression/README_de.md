# TF Segmentierte Lineare Regressions-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie wendet einen linearen Regressionskanal innerhalb jedes Zeitsegments an. Eine Long-Position wird eröffnet, wenn der Preis die obere Bande nach oben kreuzt, und eine Short-Position, wenn er die untere Bande nach unten kreuzt.

## Details
- **Einstiegskriterien**: Preis kreuzt den Regressionskanal.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Kreuzung der gegenüberliegenden Bande.
- **Stops**: Nein.
- **Standardwerte**:
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `Segment` = TimeSpan.FromDays(1)
  - `Multiplier` = 2
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Linear Regression
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
