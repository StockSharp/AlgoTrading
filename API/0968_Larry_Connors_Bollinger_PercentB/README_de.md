# Larry Connors Bollinger %B-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie folgt dem Larry Connors %B-Ansatz. Sie kauft, wenn der Preis in einem Aufwärtstrend über der 200-Perioden-SMA liegt und der Bollinger %B-Wert drei aufeinanderfolgende Kerzen unter einem Schwellenwert bleibt. Positionen werden geschlossen, wenn %B über einen oberen Schwellenwert steigt.

Die Standardkonfiguration zielt auf Tageskerzen ab.

## Details

- **Einstiegskriterien**: Schlusskurs über SMA200 und %B unter `LowPercentB` für drei aufeinanderfolgende Kerzen.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: %B kreuzt über `HighPercentB` oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `SmaPeriod` = 200
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `LowPercentB` = 0.2m
  - `HighPercentB` = 0.8m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromDays(1)
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Long
  - Indikatoren: Bollinger Bands, SMA
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
