# S&P 100 Optionsverfall-Wochen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kauft zu Beginn der Optionsverfall-Woche (die Woche, die den dritten Freitag des Monats enthält) und schließt die Position an diesem dritten Freitag.

## Details

- **Einstiegskriterien**:
  - **Long**: eine Long-Position am Montag der Optionsverfall-Woche eröffnen.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**:
  - die Long-Position am dritten Freitag des Monats schließen.
- **Stops**: Nein.
- **Standardwerte**:
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame().
- **Filter**:
  - Kategorie: Saisonalität
  - Richtung: Nur Long
  - Indikatoren: Keine
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Täglich
  - Saisonalität: Ja
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
