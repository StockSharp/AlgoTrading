# Three Signal Directional Trend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Three Signal Directional Trend-Strategie kombiniert MACD, den Stochastik-Oszillator und die Rate of Change des gleitenden Durchschnitts zur Bestimmung der Trendrichtung. Jeder Indikator stimmt für Long- oder Short-Bedingungen, und Positionen werden eröffnet, wenn mindestens zwei Indikatoren übereinstimmen. Die Methode zielt darauf ab, breite Richtungsbewegungen zu erfassen und dabei Rauschen durch mehrere Bestätigungssignale zu filtern.

## Details

- **Einstiegskriterien:**
  - Mindestens zwei von drei Signalen stimmen überein.
  - **Long**: MACD-Signal steigend, Stochastik unter Überverkauf, MA ROC positiv.
  - **Short**: MACD-Signal fallend, Stochastik über Überkauf, MA ROC negativ.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Gegenteiliges Signal.
- **Stops**: Keine.
- **Standardwerte**:
  - `AvgLength` = 50
  - `RocLength` = 1
  - `AvgRocLength` = 10
  - `StochLength` = 14
  - `SmoothK` = 3
  - `Overbought` = 80
  - `Oversold` = 20
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdAvgLength` = 9
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: MACD, Stochastic, SMA, ROC
  - Stops: Keine
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
