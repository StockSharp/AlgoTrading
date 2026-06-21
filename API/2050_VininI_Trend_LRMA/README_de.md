# VininI Trend LRMA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die VininI Trend LRMA-Strategie verwendet einen Linear Regression Moving Average (LRMA), um die Marktrichtung zu verfolgen. Die Strategie unterstützt zwei Einstiegsmodi:
- **Breakdown**: handelt, wenn LRMA feste obere oder untere Niveaus kreuzt.
- **Twist**: handelt, wenn LRMA die Richtung umkehrt.

## Details

- **Einstiegskriterien**: LRMA kreuzt Niveaus (Breakdown) oder ändert die Richtung (Twist)
- **Long/Short**: Beide
- **Ausstiegskriterien**: Entgegengesetztes Signal
- **Stops**: Keine
- **Standardwerte**:
  - `CandleType` = TimeFrameCandle 4h
  - `Period` = 13
  - `UpLevel` = 10
  - `DnLevel` = -10
  - `Mode` = Breakdown
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: LinearRegression
  - Stops: Keine
  - Komplexität: Grundlegend
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
