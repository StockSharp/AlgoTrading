# Quadratische-Regressions-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie berechnet eine quadratische Regressionslinie für die letzten `Length` Bars und handelt auf Basis der Kurskreuzungen mit der Regressionslinie.

## Details

- **Einstiegskriterien**: Kurs kreuzt die quadratische Regressionslinie nach oben/unten.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Entgegengesetzter Crossover.
- **Stops**: Nein.
- **Standardwerte**:
  - `Length` = 54.
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Quadratic Regression
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
