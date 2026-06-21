# Tuga Supertrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Tuga Supertrend ist eine reine Long-Strategie, die auf dem SuperTrend-Indikator basiert. Sie eröffnet eine Long-Position, wenn die SuperTrend-Richtung nach unten wechselt, und schließt sie, wenn die Richtung nach oben dreht.

## Details
- **Daten**: Preiskerzen.
- **Einstiegskriterien**:
  - **Long**: SuperTrend-Richtung wechselt innerhalb des Datumsfensters von aufwärts nach abwärts.
- **Ausstiegskriterien**: SuperTrend-Richtung wechselt von abwärts nach aufwärts.
- **Stops**: Keine.
- **Standardwerte**:
  - `StartDate` = 2018-01-01
  - `EndDate` = 2069-12-31
  - `AtrPeriod` = 10
  - `Factor` = 3.0
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Nur Long
  - Indikatoren: SuperTrend, ATR
  - Komplexität: Niedrig
  - Risikolevel: Mittel
