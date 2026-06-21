# Consecutive Close High1 Mean-Reversion-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Nur-Short-Strategie, die aufeinanderfolgende Schlusskurse über dem vorherigen Hoch zählt und verkauft, sobald die Anzahl einen Schwellenwert erreicht. Die Position wird geschlossen, wenn der Preis unter das vorherige Tief fällt. Der optionale 200 EMA-Filter bestätigt den Abwärtstrend.

## Details

- **Einstiegskriterien**: aufeinanderfolgende Schlusskurse über dem vorherigen Hoch erreichen den Schwellenwert
- **Long/Short**: Short
- **Ausstiegskriterien**: Schlusskurs unter dem vorherigen Tief
- **Stops**: Nein
- **Standardwerte**:
  - `Threshold` = 3
  - `EmaPeriod` = 200
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Short
  - Indikatoren: EMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
