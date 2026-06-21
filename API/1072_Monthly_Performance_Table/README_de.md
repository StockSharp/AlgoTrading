# Monatliche Leistungstabellen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Handelt, wenn der ADX zwischen +DI und -DI liegt und beide Differenzen vom ADX konfigurierbare Schwellenwerte überschreiten.

## Details

- **Einstiegskriterien**:
  - Long, wenn |+DI - ADX| ≥ `LongDifference` und |-DI - ADX| ≥ `LongDifference` mit ADX zwischen +DI und -DI.
  - Short, wenn |+DI - ADX| ≥ `ShortDifference` und |-DI - ADX| ≥ `ShortDifference` mit ADX zwischen -DI und +DI.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Gegensätzliches Signal.
- **Stops**: Nein.
- **Standardwerte**:
  - `Length` = 14
  - `LongDifference` = 10
  - `ShortDifference` = 10
  - `CandleType` = TimeSpan.FromHours(1)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: ADX, DMI
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
