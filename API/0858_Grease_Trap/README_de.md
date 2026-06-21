# Grease Trap
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Grease Trap verwendet zwei gleitende Durchschnitte mit Fibonacci-Längen und handelt deren Kreuzungen mit Gewinnzielen.

## Details
- **Daten**: Kurskerzen.
- **Einstiegskriterien**:
  - **Long**: Der schnelle Durchschnitt kreuzt den langsamen von unten nach oben.
  - **Short**: Der schnelle Durchschnitt kreuzt den langsamen von oben nach unten.
- **Ausstiegskriterien**: Gewinnziel oder entgegengesetzter Kreuzungspunkt.
- **Stops**: Keine.
- **Standardwerte**:
  - `Length1` = 9
  - `Length2` = 14
  - `LongProfit` = 0.02
  - `ShortProfit` = 0.02
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Long & Short
  - Indikatoren: SMA
  - Komplexität: Niedrig
  - Risikolevel: Mittel
