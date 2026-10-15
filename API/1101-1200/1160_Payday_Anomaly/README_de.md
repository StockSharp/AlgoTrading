# Payday-Anomalie-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie eröffnet an ausgewählten Zahltagen (1., 2., 16. und 31. eines jeden Monats) eine Long-Position und schließt die Position am darauffolgenden Tag.

## Details

- **Einstiegskriterien**:
  - **Long**: Long-Position an ausgewählten Tagen des Monats eröffnen.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**:
  - Long-Position schließen, wenn der Tag nicht ausgewählt ist.
- **Stops**: Nein.
- **Standardwerte**:
  - `Trade1st` = true.
  - `Trade2nd` = true.
  - `Trade16th` = true.
  - `Trade31st` = true.
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
