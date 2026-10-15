# Molly ETF EMA Crossover-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie eröffnet eine Long-Position, wenn die schnelle EMA die langsame EMA von unten kreuzt, und schließt sie, wenn die schnelle EMA die langsame EMA von oben kreuzt. Sie enthält optionale Parameter zur Einschränkung des Handels auf einen bestimmten Datumsbereich.

## Details

- **Einstiegskriterien**:
  - **Long**: Die schnelle EMA kreuzt die langsame EMA nach oben innerhalb des Datumsbereichs.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**:
  - Die schnelle EMA kreuzt die langsame EMA nach unten oder der Datumsbereich endet.
- **Stops**: Keine.
- **Standardwerte**:
  - `Fast EMA` = 10
  - `Slow EMA` = 21
  - `Start Date` = 2018-01-01
  - `End Date` = 2023-09-07
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Nur Long
  - Indikatoren: EMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
