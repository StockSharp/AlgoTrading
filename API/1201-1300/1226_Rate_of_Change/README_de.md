# Strategie mit Veränderungsrate (Rate of Change)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie verwendet den Rate-of-Change-Indikator, um Blasenbedingungen zu erkennen und Nulllinienkreuzungen mit dynamischer Positionsgröße zu handeln.

Backtests zeigen stabile Performance auf Tagesdaten für Hauptassets.

## Details

- **Einstiegskriterien**: ROC kreuzt über oder unter null; optionaler Short beim Platzen der Blase.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegenläufiges Signal oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `RocLength` = 365
  - `BubbleThreshold` = 180m
  - `StopLossPercent` = 6m
  - `FixedRatioValue` = 400m
  - `IncreasingOrderAmount` = 200m
  - `CandleType` = TimeSpan.FromDays(1)
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Beide
  - Indikatoren: RateOfChange
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
