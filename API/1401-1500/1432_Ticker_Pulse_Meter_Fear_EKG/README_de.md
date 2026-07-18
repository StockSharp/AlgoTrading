# Ticker Pulse Meter + Fear EKG Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Kombiniert kurze und lange Rückblickperioden, um überverkaufte Bedingungen und Erholungen zu erkennen.
Kauft, wenn der kombinierte Perzentilwert den oberen Auslöser kreuzt, und steigt aus bei einem Gewinnmitnahme-Kreuz.

## Details

- **Einstiegskriterien**: Perzentil kreuzt über `EntryThresholdHigh` oder unter `OrangeEntryThreshold`
- **Long/Short**: Nur Long
- **Ausstiegskriterien**: Kreuzung unter `ProfitTake`
- **Stops**: Nein
- **Standardwerte**:
  - `LookbackShort` = 50
  - `LookbackLong` = 200
  - `ProfitTake` = 95
  - `EntryThresholdHigh` = 20
  - `EntryThresholdLow` = 40
  - `OrangeEntryThreshold` = 95
- **Filter**:
  - Kategorie: Oszillator
  - Richtung: Long
  - Indikatoren: Highest, Lowest
  - Stops: Nein
  - Komplexität: Fortgeschritten
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
