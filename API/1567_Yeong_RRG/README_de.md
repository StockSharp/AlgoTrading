# Yeong RRG
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie auf Basis der normalisierten relativen Stärke und des Momentum-Verhältnisses (RRG).

Die Strategie geht Long, wenn sowohl JDK RS als auch JDK RoC über 100 liegen, und verlässt die Position, wenn beide unter 100 fallen.

## Details

- **Einstiegskriterien**: JDK RS und JDK RoC über 100.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: JDK RS und JDK RoC unter 100.
- **Stops**: Nein.
- **Standardwerte**:
  - `Length` = 14
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Relative Strength
  - Richtung: Long
  - Indikatoren: SMA, ROC, StandardDeviation
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

