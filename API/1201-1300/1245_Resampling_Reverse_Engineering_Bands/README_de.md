# Resampling Reverse-Engineering-Bänder
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Resampling Reverse Engineering Bands rekonstruieren RSI-Preisniveaus mit einer konfigurierbaren Resampling-Rate. Die Strategie kauft, wenn der Preis unter das untere Band fällt, und verkauft, wenn der Preis über das obere Band steigt.

## Details
- **Daten**: Preiskerzen.
- **Einstiegskriterien**:
  - **Long**: Schlusskurs kreuzt unter das untere RRSI-Band.
  - **Short**: Schlusskurs kreuzt über das obere RRSI-Band.
- **Ausstiegskriterien**: entgegengesetztes Signal.
- **Stops**: Keine.
- **Standardwerte**:
  - `RsiPeriod` = 14
  - `HighThreshold` = 70
  - `LowThreshold` = 30
  - `SampleLength` = 1
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Long & Short
  - Indikatoren: RSI
  - Komplexität: Moderat
  - Risikolevel: Mittel
