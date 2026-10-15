# Hochs-Tiefs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die auf Kerzen-Mittelpunkten relativ zur Hoch-Tief-Spanne handelt.

Kauft, wenn der aktuelle Kerzen-Mittelpunkt unter dem Durchschnitt der höchsten und niedrigsten Werte liegt und der normierte Abstand unter LowThreshold fällt. Schließt die Long-Position, wenn der Mittelpunkt über den Durchschnitt steigt und der normierte Abstand über HighThreshold liegt.

## Details

- **Einstiegskriterien**: Mittelpunkt unter Durchschnitt und normierter Abstand unter LowThreshold.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Mittelpunkt über Durchschnitt und normierter Abstand über HighThreshold.
- **Stops**: Nein.
- **Standardwerte**:
  - `Range` = 100
  - `LowThreshold` = 15m
  - `HighThreshold` = 85m
  - `CandleType` = TimeSpan.FromMinutes(240)
- **Filter**:
  - Kategorie: Range
  - Richtung: Long
  - Indikatoren: Highest, Lowest
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (240m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
