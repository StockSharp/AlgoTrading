# Strategie Lineare Regression mit Allen Daten
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie berechnet eine lineare Regressionslinie mithilfe aller verfügbaren Balken und zeichnet sie auf dem Chart.
Außerdem werden Steigung, Achsenabschnitt und Korrelationskoeffizienten protokolliert.

## Details

- **Einstiegskriterien**: Keine.
- **Long/Short**: Keine.
- **Ausstiegskriterien**: Keine.
- **Stops**: Nein.
- **Standardwerte**:
  - `MaxBarsBack` = 5000.
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
- **Filter**:
  - Kategorie: Hilfsprogramm
  - Richtung: Keine
  - Indikatoren: Linear Regression
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
