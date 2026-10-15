# Montags-Eröffnungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kauft zu Beginn der Woche und schließt die Position am Dienstag innerhalb eines festgelegten Jahreszeitraums.

## Details

- **Einstiegskriterien**:
  - **Long**: Long-Position am Montag eröffnen.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**:
  - Long-Position am Dienstag schließen.
- **Stops**: Nein.
- **Standardwerte**:
  - `StartYear` = 2023.
  - `EndYear` = 2025.
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame().
- **Filter**:
  - Kategorie: Saisonalität
  - Richtung: Long
  - Indikatoren: Keine
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Täglich
  - Saisonalität: Ja
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
