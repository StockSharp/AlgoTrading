# Gaussian-Anomalie-Ableitungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Verwendet einen gleitenden Durchschnitt der Preisanomalie `1 - (high + low) / (2 * close)` und deren geglättete Ableitung.
Handelt Long, wenn die Ableitung ihren positiven Schwellenwert überschreitet, und Short, wenn sie unter den negativen Schwellenwert fällt.

## Details

- **Einstiegskriterien**: Anomalie oder deren Ableitung überschreitet Schwellenwert
- **Long/Short**: Konfigurierbar
- **Ausstiegskriterien**: entgegengesetztes Signal
- **Stops**: Nein
- **Standardwerte**:
  - `UseSma` = true
  - `MaPeriod` = 3
  - `DerivativeMaPeriod` = 2
  - `ThresholdCoeff` = 1.0
  - `DerivativeThresholdCoeff` = 1.0
  - `StartBarCount` = 600
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: SMA, EMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
